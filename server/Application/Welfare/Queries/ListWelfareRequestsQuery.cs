using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Welfare.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Welfare.Queries;

public record ListWelfareRequestsQuery(
    Guid TenantId,
    int Page,
    int Limit,
    string? MemberId,
    string? Status,
    string? Type
) : IRequest<PagedResult<WelfareRequestDto>>;

public class ListWelfareRequestsQueryHandler(AppDbContext db)
    : IRequestHandler<ListWelfareRequestsQuery, PagedResult<WelfareRequestDto>>
{
    public async Task<PagedResult<WelfareRequestDto>> Handle(ListWelfareRequestsQuery request, CancellationToken ct)
    {
        var query = db.WelfareRequests
            .Include(w => w.Member).ThenInclude(m => m.User)
            .Where(w => w.TenantId == request.TenantId && w.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.MemberId) && Guid.TryParse(request.MemberId, out var memberId))
            query = query.Where(w => w.MemberId == memberId);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(w => w.Status == request.Status);

        if (!string.IsNullOrWhiteSpace(request.Type))
            query = query.Where(w => w.Type == request.Type);

        var total = await query.CountAsync(ct);

        var raw = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(ct);
        var items = raw.Select(w => ToDto(w)).ToList();

        return PagedResult<WelfareRequestDto>.Create(items, request.Page, request.Limit, total);
    }

    internal static WelfareRequestDto ToDto(Domain.Entities.WelfareRequest w) => new(
        w.Id.ToString(), w.TenantId.ToString(), w.MemberId.ToString(),
        $"{w.Member.User.FirstName} {w.Member.User.LastName}", w.Member.MembershipNumber,
        w.Type, w.Description, w.AmountRequested, w.AmountApproved, w.AmountPaid,
        w.Status, w.RejectionReason, w.Notes,
        w.ReviewedAt?.ToString("O"), w.PaidAt?.ToString("O"),
        w.CreatedAt.ToString("O"), w.UpdatedAt?.ToString("O"));
}
