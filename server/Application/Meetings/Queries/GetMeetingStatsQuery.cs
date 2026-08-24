using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Meetings.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Meetings.Queries;

public record GetMeetingStatsQuery(Guid TenantId) : IRequest<MeetingStatsDto>;

public class GetMeetingStatsQueryHandler(AppDbContext db)
    : IRequestHandler<GetMeetingStatsQuery, MeetingStatsDto>
{
    public async Task<MeetingStatsDto> Handle(GetMeetingStatsQuery request, CancellationToken ct)
    {
        var meetings = await db.Meetings
            .Where(m => m.TenantId == request.TenantId && m.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Scheduled = g.Count(m => m.Status == "scheduled"),
                InProgress = g.Count(m => m.Status == "in_progress"),
                Completed = g.Count(m => m.Status == "completed"),
                Cancelled = g.Count(m => m.Status == "cancelled"),
            })
            .FirstOrDefaultAsync(ct);

        return meetings is null
            ? new MeetingStatsDto(0, 0, 0, 0, 0)
            : new MeetingStatsDto(meetings.Total, meetings.Scheduled, meetings.InProgress, meetings.Completed, meetings.Cancelled);
    }
}
