using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using server.Application.Common;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;
using System.Text.Json;

namespace server.Application.TenantSettings;

public record UpdateTenantSettingsRequest(
    string? Name,
    string? BaseCurrency,
    string? CurrencySymbol,
    string? CountryCode,
    string? LogoUrl
);

public record UpdateTenantSettingsCommand(Guid TenantId, UpdateTenantSettingsRequest Body, Guid? UserId = null)
    : IRequest<ServiceResult<TenantSettingsDto>>;

public class UpdateTenantSettingsHandler(AppDbContext db, IWebHostEnvironment env, IConfiguration config,
    CurrencyValidationService currencyValidation, AuditService audit)
    : IRequestHandler<UpdateTenantSettingsCommand, ServiceResult<TenantSettingsDto>>
{
    public async Task<ServiceResult<TenantSettingsDto>> Handle(UpdateTenantSettingsCommand req, CancellationToken ct)
    {
        Log.Information("UpdateTenantSettings TenantId={TenantId}", req.TenantId);
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == req.TenantId, ct);
        if (tenant is null) return ServiceResult<TenantSettingsDto>.Failure("Tenant not found", "errors.tenant.not_found");
        var previousCountry = tenant.CountryCode;
        var previousCurrency = tenant.BaseCurrency;

        CurrencyValidationResponse? validation = null;
        if (req.Body.CountryCode is not null || req.Body.BaseCurrency is not null)
        {
            validation = await currencyValidation.ValidateAsync(new(
                req.Body.CountryCode ?? tenant.CountryCode, req.Body.BaseCurrency ?? tenant.BaseCurrency), ct);
            if (!validation.IsConsistent)
                return ServiceResult<TenantSettingsDto>.Failure(
                    "The currency does not match the association country.", "errors.tenant.currency_mismatch");
        }
        if (req.Body.Name is not null)           tenant.Name           = req.Body.Name;
        if (validation is not null)             tenant.BaseCurrency   = validation.BaseCurrency;
        if (req.Body.CurrencySymbol is not null) tenant.CurrencySymbol = req.Body.CurrencySymbol;
        if (validation is not null)             tenant.CountryCode    = validation.CountryCode;
        if (validation is not null && req.Body.CurrencySymbol is null && previousCurrency != tenant.BaseCurrency)
            tenant.CurrencySymbol = tenant.BaseCurrency;
        if (req.UserId.HasValue &&
            (previousCountry != tenant.CountryCode || previousCurrency != tenant.BaseCurrency))
            audit.Log("tenant_currency_configuration_updated", req.UserId.Value, tenant.Id,
                "tenant", tenant.Id.ToString(), JsonSerializer.Serialize(new
                {
                    previous_country = previousCountry, previous_currency = previousCurrency,
                    country = tenant.CountryCode, currency = tenant.BaseCurrency
                }));

        // LogoUrl=null = suppression intentionnelle du logo
        if (req.Body.LogoUrl != tenant.LogoUrl)
        {
            if (tenant.LogoUrl is not null && req.Body.LogoUrl is null)
                DeletePhysicalFile(tenant.LogoUrl);
            tenant.LogoUrl = req.Body.LogoUrl;
        }

        tenant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return ServiceResult<TenantSettingsDto>.Success(new TenantSettingsDto(
            tenant.Name, tenant.Slug, tenant.BaseCurrency, tenant.CurrencySymbol,
            tenant.CountryCode, tenant.LogoUrl, tenant.SubscriptionTier, tenant.SubscriptionStatus, validation));
    }

    private void DeletePhysicalFile(string fileUrl)
    {
        try
        {
            var urlPrefix = (config["FileStorage:UrlPrefix"]?.TrimEnd('/') ?? "").TrimEnd('/');
            var relativePath = urlPrefix.Length > 0 && fileUrl.StartsWith(urlPrefix)
                ? fileUrl[urlPrefix.Length..].TrimStart('/')
                : new Uri(fileUrl).AbsolutePath.TrimStart('/');

            var fullPath = Path.Combine(env.ContentRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch
        {
            // suppression best-effort : on ne bloque pas la mise à jour si le fichier est introuvable
        }
    }
}
