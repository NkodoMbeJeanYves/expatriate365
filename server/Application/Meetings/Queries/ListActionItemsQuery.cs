using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Meetings.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Meetings.Queries;

public record ListActionItemsQuery(Guid TenantId, Guid MeetingId) : IRequest<List<ActionItemDto>>;

public class ListActionItemsQueryHandler(AppDbContext db)
    : IRequestHandler<ListActionItemsQuery, List<ActionItemDto>>
{
    public async Task<List<ActionItemDto>> Handle(ListActionItemsQuery request, CancellationToken ct)
    {
        var items = await db.MeetingActionItems
            .AsNoTracking()
            .Include(i => i.AssignedTo).ThenInclude(m => m!.User)
            .Where(i => i.TenantId == request.TenantId && i.MeetingId == request.MeetingId && i.IsActive)
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.CreatedAt)
            .ToListAsync(ct);

        return items.Select(i => new ActionItemDto(
            i.Id.ToString(), i.MeetingId.ToString(),
            i.Title, i.Description,
            i.AssignedToMemberId?.ToString(),
            i.AssignedTo != null ? i.AssignedTo.User.FirstName + " " + i.AssignedTo.User.LastName : null,
            i.DueDate?.ToString("yyyy-MM-dd"), i.Status,
            i.CreatedAt.ToString("O"), i.UpdatedAt?.ToString("O")
        )).ToList();
    }
}
