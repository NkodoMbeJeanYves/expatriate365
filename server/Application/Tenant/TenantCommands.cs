using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.TenantSettings;

public record UpdateTenantSettingsRequest(
    string? Name,
    string? BaseCurrency,
    string? CurrencySymbol,
    string? CountryCode,
    string? LogoUrl
);

public record UpdateTenantSettingsCommand(Guid TenantId, UpdateTenantSettingsRequest Body)
    : IRequest<ServiceResult<TenantSettingsDto>>;

public class UpdateTenantSettingsHandler(AppDbContext db)
    : IRequestHandler<UpdateTenantSettingsCommand, ServiceResult<TenantSettingsDto>>
{
    public async Task<ServiceResult<TenantSettingsDto>> Handle(UpdateTenantSettingsCommand req, CancellationToken ct)
    {
        Log.Information("UpdateTenantSettings TenantId={TenantId}", req.TenantId);
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == req.TenantId, ct);
        if (tenant is null) return ServiceResult<TenantSettingsDto>.Failure("Tenant not found");

        if (req.Body.Name is not null)           tenant.Name           = req.Body.Name;
        if (req.Body.BaseCurrency is not null)   tenant.BaseCurrency   = req.Body.BaseCurrency;
        if (req.Body.CurrencySymbol is not null) tenant.CurrencySymbol = req.Body.CurrencySymbol;
        if (req.Body.CountryCode is not null)    tenant.CountryCode    = req.Body.CountryCode;
        if (req.Body.LogoUrl is not null)        tenant.LogoUrl        = req.Body.LogoUrl;
        tenant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return ServiceResult<TenantSettingsDto>.Success(new TenantSettingsDto(
            tenant.Name, tenant.Slug, tenant.BaseCurrency, tenant.CurrencySymbol,
            tenant.CountryCode, tenant.LogoUrl, tenant.SubscriptionTier, tenant.SubscriptionStatus));
    }
}
