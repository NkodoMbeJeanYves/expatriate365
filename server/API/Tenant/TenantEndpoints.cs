using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.TenantSettings;
using server.Infrastructure.Persistence;
using System.Security.Claims;

namespace server.API.Tenant;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this WebApplication app)
    {
        // Public — no auth required, used by super_admin tenant picker
        app.MapGet("/api/v1/tenants/public", async (AppDbContext db) =>
        {
            var tenants = await db.Tenants
                .AsNoTracking()
                .Where(t => t.IsActive)
                .Select(t => new { t.Id, t.Name, t.Slug, t.LogoUrl })
                .OrderBy(t => t.Name)
                .ToListAsync();
            return Results.Ok(tenants);
        })
        .WithTags("Tenants")
        .WithName("GetPublicTenants");

        var g = app.MapGroup("/api/v1/tenant").RequireAuthorization();
        g.MapPost("/currency-validation", async (CurrencyValidationRequest body,
            CurrencyValidationService service, CancellationToken ct) =>
            TypedResults.Ok(await service.ValidateAsync(body, ct)))
            .RequireAuthorization(Permissions.SettingsUpdate)
            .WithName("ValidateTenantCurrency")
            .WithSummary("Validate association country/currency and Frankfurter catalogue coverage")
            .WithDescription("""
                Coverage does not establish local currency validity or availability of a current exchange rate.
                Example request: {"country_code":"MU","base_currency":"MUR"}.
                Example response: {"country_code":"MU","base_currency":"MUR","is_consistent":true,
                "allowed_currencies":["MUR"],"coverage_status":"covered","source":"Frankfurter",
                "checked_at":"2026-10-22T00:00:00Z"}.
                A provider failure returns coverage_status "unavailable", never "not_covered".
                """)
            .Produces<CurrencyValidationResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        g.MapGet("/settings", async (HttpContext ctx, IMediator mediator) =>
        {
            var tenantId = GetTenantId(ctx);
            var result = await mediator.Send(new GetTenantSettingsQuery(tenantId));
            return result is null ? Results.NotFound(new { error = "Tenant not found" }) : Results.Ok(result);
        })
        .WithName("GetTenantSettings")
        .RequireAuthorization(Permissions.SettingsRead);

        g.MapPut("/settings", async (HttpContext ctx, [FromBody] UpdateTenantSettingsRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = GetTenantId(ctx);
            if (!Guid.TryParse(ctx.User.FindFirstValue("sub") ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();
            var result = await mediator.Send(new UpdateTenantSettingsCommand(tenantId, body, userId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
        })
        .RequireAuthorization(Permissions.SettingsUpdate)
        .WithName("UpdateTenantSettings")
        .WithSummary("Update association settings after validating local country/currency consistency")
        .WithDescription("""
            Inconsistent country/currency returns {"error":"errors.tenant.currency_mismatch"} without mutation.
            Valid monetary updates include currency_validation in the response, even if Frankfurter
            coverage is not_covered or unavailable. Example monetary request:
            {"country_code":"RS","base_currency":"RSD","currency_symbol":"RSD"}.
            No existing financial amount is converted by this operation.
            """)
        .Produces<TenantSettingsDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
;
    }

    private static Guid GetTenantId(HttpContext ctx)
    {
        var claim = ctx.User.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
