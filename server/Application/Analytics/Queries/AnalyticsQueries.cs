using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Analytics.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Analytics.Queries;

public record GetAnalyticsOverviewQuery(Guid TenantId) : IRequest<AnalyticsOverviewDto>;

public class GetAnalyticsOverviewQueryHandler(AppDbContext db)
    : IRequestHandler<GetAnalyticsOverviewQuery, AnalyticsOverviewDto>
{
    public async Task<AnalyticsOverviewDto> Handle(GetAnalyticsOverviewQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var memberStats = await db.Members
            .Where(m => m.TenantId == request.TenantId && m.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total        = g.Count(),
                Active       = g.Count(m => m.Status == "active"),
                NewThisMonth = g.Count(m => m.CreatedAt >= startOfMonth),
            })
            .FirstOrDefaultAsync(ct);

        var totalMembers  = memberStats?.Total        ?? 0;
        var activeMembers = memberStats?.Active       ?? 0;
        var newThisMonth  = memberStats?.NewThisMonth ?? 0;

        var financeStats = await db.ContributionCharges
            .Where(c => c.TenantId == request.TenantId && c.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new { Collected = g.Sum(c => c.AmountPaid), Expected = g.Sum(c => c.BaseAmount + c.PenaltyAmount - c.WaiverAmount) })
            .FirstOrDefaultAsync(ct);

        var eventStats = await db.Events
            .Where(e => e.TenantId == request.TenantId && e.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Upcoming = g.Count(e => e.StartDate > now) })
            .FirstOrDefaultAsync(ct);

        var totalEvents    = eventStats?.Total    ?? 0;
        var upcomingEvents = eventStats?.Upcoming ?? 0;
        var totalMeetings = await db.Meetings.CountAsync(m => m.TenantId == request.TenantId && m.IsActive, ct);
        var totalElections = await db.Elections.CountAsync(e => e.TenantId == request.TenantId && e.IsActive, ct);

        return new AnalyticsOverviewDto(
            totalMembers, activeMembers, newThisMonth,
            financeStats?.Collected ?? 0, (financeStats?.Expected ?? 0) - (financeStats?.Collected ?? 0),
            totalEvents, upcomingEvents, totalMeetings, totalElections);
    }
}

public record GetMemberAnalyticsQuery(Guid TenantId) : IRequest<MemberAnalyticsDto>;

public class GetMemberAnalyticsQueryHandler(AppDbContext db)
    : IRequestHandler<GetMemberAnalyticsQuery, MemberAnalyticsDto>
{
    public async Task<MemberAnalyticsDto> Handle(GetMemberAnalyticsQuery request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddMonths(-12);

        var monthly = await db.Members
            .Where(m => m.TenantId == request.TenantId && m.IsActive && m.CreatedAt >= since)
            .GroupBy(m => new { m.CreatedAt.Year, m.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync(ct);

        var byStatus = await db.Members
            .Where(m => m.TenantId == request.TenantId && m.IsActive)
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new MemberAnalyticsDto(
            monthly.Select(m => new MonthlySeriesPoint($"{m.Year:0000}-{m.Month:00}", m.Count)).ToList(),
            byStatus.Select(s => new MembersByStatusDto(s.Status, s.Count)).ToList());
    }
}

public record GetFinanceAnalyticsQuery(Guid TenantId) : IRequest<FinanceAnalyticsDto>;

public class GetFinanceAnalyticsQueryHandler(AppDbContext db)
    : IRequestHandler<GetFinanceAnalyticsQuery, FinanceAnalyticsDto>
{
    public async Task<FinanceAnalyticsDto> Handle(GetFinanceAnalyticsQuery request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddMonths(-12);

        var monthly = await db.Payments
            .Where(p => p.TenantId == request.TenantId && p.IsActive && p.Status == "confirmed"
                && p.CreatedAt >= since)
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(p => p.Amount) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync(ct);

        var monthlyExpected = await db.ContributionCharges
            .Where(c => c.TenantId == request.TenantId && c.IsActive && c.CreatedAt >= since)
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(c => c.BaseAmount + c.PenaltyAmount - c.WaiverAmount) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync(ct);

        var totals = await db.ContributionCharges
            .Where(c => c.TenantId == request.TenantId && c.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new { Collected = g.Sum(c => c.AmountPaid), Expected = g.Sum(c => c.BaseAmount + c.PenaltyAmount - c.WaiverAmount) })
            .FirstOrDefaultAsync(ct);

        var collected = totals?.Collected ?? 0;
        var expected = totals?.Expected ?? 0;
        var rate = expected > 0 ? Math.Round(collected / expected * 100, 1) : 0;

        return new FinanceAnalyticsDto(
            monthly.Select(m => new MonthlySeriesPoint($"{m.Year:0000}-{m.Month:00}", m.Amount)).ToList(),
            monthlyExpected.Select(m => new MonthlySeriesPoint($"{m.Year:0000}-{m.Month:00}", m.Amount)).ToList(),
            collected, expected, rate);
    }
}

public record GetEngagementAnalyticsQuery(Guid TenantId) : IRequest<EngagementAnalyticsDto>;

public class GetEngagementAnalyticsQueryHandler(AppDbContext db)
    : IRequestHandler<GetEngagementAnalyticsQuery, EngagementAnalyticsDto>
{
    public async Task<EngagementAnalyticsDto> Handle(GetEngagementAnalyticsQuery request, CancellationToken ct)
    {
        var attendanceStats = await db.MeetingAttendances
            .Where(a => a.TenantId == request.TenantId && a.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Present = g.Count(a => a.Status == "present") })
            .FirstOrDefaultAsync(ct);

        var totalAttendance   = attendanceStats?.Total   ?? 0;
        var presentAttendance = attendanceStats?.Present ?? 0;
        var meetingRate = totalAttendance > 0 ? Math.Round((decimal)presentAttendance / totalAttendance * 100, 1) : 0;

        var totalVoters  = await db.ElectionVotes.CountAsync(v => v.TenantId == request.TenantId, ct);
        var totalMembers = await db.Members.CountAsync(m => m.TenantId == request.TenantId && m.IsActive && m.Status == "active", ct);
        var electionRate = totalMembers > 0 ? Math.Round((decimal)totalVoters / totalMembers * 100, 1) : 0;

        var eventRegistrations = await db.EventRegistrations.CountAsync(r => r.TenantId == request.TenantId && r.IsActive, ct);

        return new EngagementAnalyticsDto(meetingRate, electionRate, eventRegistrations);
    }
}
