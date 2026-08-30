using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.TenantSettings;
using server.Infrastructure.Persistence;

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

        g.MapGet("/settings", async (HttpContext ctx, IMediator mediator) =>
        {
            var tenantId = GetTenantId(ctx);
            var result = await mediator.Send(new GetTenantSettingsQuery(tenantId));
            return result is null ? Results.NotFound(new { error = "Tenant not found" }) : Results.Ok(result);
        })
        .WithName("GetTenantSettings")
;

        g.MapPut("/settings", async (HttpContext ctx, [FromBody] UpdateTenantSettingsRequest body, IMediator mediator) =>
        {
            var tenantId = GetTenantId(ctx);
            var result = await mediator.Send(new UpdateTenantSettingsCommand(tenantId, body));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .RequireAuthorization(Permissions.SettingsUpdate)
        .WithName("UpdateTenantSettings")
;
    }

    private static Guid GetTenantId(HttpContext ctx)
    {
        var claim = ctx.User.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
