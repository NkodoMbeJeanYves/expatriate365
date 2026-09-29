using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.SuperAdmin;

public record ListTenantsQuery : IRequest<ServiceResult<List<TenantSummaryDto>>>;

public class ListTenantsQueryHandler(AppDbContext db, ILogger<ListTenantsQueryHandler> log)
    : IRequestHandler<ListTenantsQuery, ServiceResult<List<TenantSummaryDto>>>
{
    public async Task<ServiceResult<List<TenantSummaryDto>>> Handle(ListTenantsQuery request, CancellationToken ct)
    {
        log.LogInformation("SuperAdmin listing all tenants");

        var tenants = await db.Tenants
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id, t.Name, t.Slug, t.CountryCode, t.BaseCurrency,
                t.IsActive, t.CreatedAt, t.UpdatedAt,
                UserCount = db.Users.Count(u => u.TenantId == t.Id && u.IsActive),
                Admin = db.Users
                    .Where(u => u.TenantId == t.Id && u.Role == "org_admin")
                    .Select(u => new { u.Email, u.FirstName, u.LastName })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var result = tenants.Select(t => new TenantSummaryDto(
            t.Id.ToString(),
            t.Name,
            t.Slug,
            t.CountryCode,
            t.BaseCurrency,
            t.IsActive,
            t.CreatedAt.ToString("O"),
            t.UpdatedAt?.ToString("O"),
            t.Admin?.Email ?? "-",
            t.Admin is null ? "-" : $"{t.Admin.FirstName} {t.Admin.LastName}",
            t.UserCount
        )).ToList();

        return ServiceResult<List<TenantSummaryDto>>.Success(result);
    }
}
