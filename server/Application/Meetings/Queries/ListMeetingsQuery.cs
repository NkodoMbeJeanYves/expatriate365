using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Meetings.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Meetings.Queries;

public record ListMeetingsQuery(
    Guid TenantId, int Page, int Limit, string? Status, string? Type
) : IRequest<PagedResult<MeetingDto>>;

public class ListMeetingsQueryHandler(AppDbContext db)
    : IRequestHandler<ListMeetingsQuery, PagedResult<MeetingDto>>
{
    public async Task<PagedResult<MeetingDto>> Handle(ListMeetingsQuery request, CancellationToken ct)
    {
        var query = db.Meetings
            .Include(m => m.Attendances)
            .Include(m => m.Minute)
            .Where(m => m.TenantId == request.TenantId && m.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(m => m.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.Type))
            query = query.Where(m => m.Type == request.Type);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.ScheduledAt)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(ct);

        return PagedResult<MeetingDto>.Create(items.Select(ToDto).ToList(), request.Page, request.Limit, total);
    }

    internal static MeetingDto ToDto(Domain.Entities.Meeting m) => new(
        m.Id.ToString(), m.TenantId.ToString(), m.Title, m.Type, m.Status,
        m.ScheduledAt.ToString("O"), m.Location, m.Agenda, m.QuorumRequired,
        m.Attendances.Count(a => a.IsActive),
        m.Attendances.Count(a => a.IsActive && (a.Status == "present" || a.Status == "proxy")),
        m.Minute is not null,
        m.StartedAt?.ToString("O"), m.EndedAt?.ToString("O"),
        m.CreatedAt.ToString("O"), m.UpdatedAt?.ToString("O"));
}
