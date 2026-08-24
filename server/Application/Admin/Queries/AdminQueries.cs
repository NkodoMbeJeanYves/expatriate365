using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Admin.DTOs;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Admin.Queries;

public record ListAdminUsersQuery(Guid TenantId, int Page, int Limit, string? Role, string? Status)
    : IRequest<PagedResult<AdminUserDto>>;

public class ListAdminUsersQueryHandler(AppDbContext db)
    : IRequestHandler<ListAdminUsersQuery, PagedResult<AdminUserDto>>
{
    public async Task<PagedResult<AdminUserDto>> Handle(ListAdminUsersQuery request, CancellationToken ct)
    {
        var query = db.Users
            .Where(u => u.TenantId == request.TenantId && u.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Role))
            query = query.Where(u => u.Role == request.Role);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(u => u.Status == request.Status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(ct);

        return PagedResult<AdminUserDto>.Create(items.Select(ToDto).ToList(), request.Page, request.Limit, total);
    }

    internal static AdminUserDto ToDto(Domain.Entities.User u) => new(
        u.Id.ToString(), u.Email, u.FullName, u.Phone,
        u.Role, u.Status, u.IsActive,
        u.LastLoginAt?.ToString("O"), u.CreatedAt.ToString("O"), u.UpdatedAt?.ToString("O"));
}

public record GetAdminStatsQuery(Guid TenantId) : IRequest<AdminStatsDto>;

public class GetAdminStatsQueryHandler(AppDbContext db)
    : IRequestHandler<GetAdminStatsQuery, AdminStatsDto>
{
    public async Task<AdminStatsDto> Handle(GetAdminStatsQuery request, CancellationToken ct)
    {
        var stats = await db.Users
            .Where(u => u.TenantId == request.TenantId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(u => u.IsActive && u.Status == "active"),
                Inactive = g.Count(u => !u.IsActive || u.Status != "active"),
            })
            .FirstOrDefaultAsync(ct);

        return stats is null
            ? new AdminStatsDto(0, 0, 0)
            : new AdminStatsDto(stats.Total, stats.Active, stats.Inactive);
    }
}
