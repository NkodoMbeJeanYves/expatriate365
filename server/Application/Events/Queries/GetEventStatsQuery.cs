using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Events.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Events.Queries;

public record GetEventStatsQuery(Guid TenantId) : IRequest<EventStatsDto>;

public class GetEventStatsQueryHandler(AppDbContext db)
    : IRequestHandler<GetEventStatsQuery, EventStatsDto>
{
    public async Task<EventStatsDto> Handle(GetEventStatsQuery request, CancellationToken ct)
    {
        var events = await db.Events
            .Where(e => e.TenantId == request.TenantId && e.IsActive)
            .Select(e => e.Status)
            .ToListAsync(ct);

        var regs = await db.EventRegistrations
            .Where(r => r.TenantId == request.TenantId && r.IsActive)
            .Select(r => r.Status)
            .ToListAsync(ct);

        return new EventStatsDto(
            TotalCount: events.Count,
            DraftCount: events.Count(s => s == "draft"),
            PublishedCount: events.Count(s => s == "published"),
            CompletedCount: events.Count(s => s == "completed"),
            CancelledCount: events.Count(s => s == "cancelled"),
            TotalRegistrations: regs.Count(s => s != "cancelled"),
            TotalAttended: regs.Count(s => s == "attended")
        );
    }
}
