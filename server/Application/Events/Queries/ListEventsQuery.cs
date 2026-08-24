using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Events.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Events.Queries;

public record ListEventsQuery(
    Guid TenantId, int Page, int Limit, string? Status, string? Type
) : IRequest<PagedResult<EventDto>>;

public class ListEventsQueryHandler(AppDbContext db)
    : IRequestHandler<ListEventsQuery, PagedResult<EventDto>>
{
    public async Task<PagedResult<EventDto>> Handle(ListEventsQuery request, CancellationToken ct)
    {
        var query = db.Events
            .Include(e => e.Registrations)
            .Where(e => e.TenantId == request.TenantId && e.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(e => e.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.Type))
            query = query.Where(e => e.Type == request.Type);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.StartDate)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(ct);

        return PagedResult<EventDto>.Create(items.Select(ToDto).ToList(), request.Page, request.Limit, total);
    }

    internal static EventDto ToDto(Domain.Entities.Event e) => new(
        e.Id.ToString(), e.TenantId.ToString(), e.Title, e.Description,
        e.Type, e.Status, e.Location,
        e.StartDate.ToString("O"), e.EndDate.ToString("O"),
        e.MaxCapacity,
        e.Registrations.Count(r => r.Status != "cancelled" && r.IsActive),
        e.Registrations.Count(r => r.Status == "attended"),
        e.IsPublic,
        e.CreatedAt.ToString("O"), e.UpdatedAt?.ToString("O"));
}
