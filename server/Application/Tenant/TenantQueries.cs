using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;

namespace server.Application.TenantSettings;

public record TenantSettingsDto(
    string Name,
    string Slug,
    string BaseCurrency,
    string CurrencySymbol,
    string CountryCode,
    string? LogoUrl,
    string SubscriptionTier,
    string SubscriptionStatus
);

public record GetTenantSettingsQuery(Guid TenantId) : IRequest<TenantSettingsDto?>;

public class GetTenantSettingsHandler(AppDbContext db)
    : IRequestHandler<GetTenantSettingsQuery, TenantSettingsDto?>
{
    public async Task<TenantSettingsDto?> Handle(GetTenantSettingsQuery req, CancellationToken ct)
    {
        var t = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.TenantId, ct);
        if (t is null) return null;
        return new TenantSettingsDto(t.Name, t.Slug, t.BaseCurrency, t.CurrencySymbol,
            t.CountryCode, t.LogoUrl, t.SubscriptionTier, t.SubscriptionStatus);
    }
}
