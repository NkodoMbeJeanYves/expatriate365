using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Communications.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Communications.Queries;

public record ListCommunicationsQuery(
    Guid TenantId, int Page, int Limit, string? Status, string? Type, string? Channel
) : IRequest<PagedResult<CommunicationDto>>;

public class ListCommunicationsQueryHandler(AppDbContext db)
    : IRequestHandler<ListCommunicationsQuery, PagedResult<CommunicationDto>>
{
    public async Task<PagedResult<CommunicationDto>> Handle(ListCommunicationsQuery request, CancellationToken ct)
    {
        var query = db.Communications
            .Include(c => c.Recipients)
            .Where(c => c.TenantId == request.TenantId && c.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(c => c.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.Type))
            query = query.Where(c => c.Type == request.Type);
        if (!string.IsNullOrWhiteSpace(request.Channel))
            query = query.Where(c => c.Channel == request.Channel);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(ct);

        return PagedResult<CommunicationDto>.Create(items.Select(ToDto).ToList(), request.Page, request.Limit, total);
    }

    internal static CommunicationDto ToDto(Domain.Entities.Communication c) => new(
        c.Id.ToString(), c.TenantId.ToString(), c.Title, c.Content,
        c.Type, c.Channel, c.Status, c.Audience,
        c.CategoryId?.ToString(), c.TargetMemberId?.ToString(),
        c.RecipientCount,
        c.Recipients.Count(r => r.Status == "read" && r.IsActive),
        c.SentAt?.ToString("O"), c.CreatedAt.ToString("O"), c.UpdatedAt?.ToString("O"));
}
