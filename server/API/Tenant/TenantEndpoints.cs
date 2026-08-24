using MediatR;
using Microsoft.AspNetCore.Mvc;
using server.Application.TenantSettings;

namespace server.API.Tenant;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/v1/tenant").RequireAuthorization();

        g.MapGet("/settings", async (HttpContext ctx, IMediator mediator) =>
        {
            var tenantId = GetTenantId(ctx);
            var result = await mediator.Send(new GetTenantSettingsQuery(tenantId));
            return result is null ? Results.NotFound(new { error = "Tenant not found" }) : Results.Ok(result);
        })
        .WithName("GetTenantSettings")
        .WithOpenApi();

        g.MapPut("/settings", async (HttpContext ctx, [FromBody] UpdateTenantSettingsRequest body, IMediator mediator) =>
        {
            var tenantId = GetTenantId(ctx);
            var result = await mediator.Send(new UpdateTenantSettingsCommand(tenantId, body));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("UpdateTenantSettings")
        .WithOpenApi();
    }

    private static Guid GetTenantId(HttpContext ctx)
    {
        var claim = ctx.User.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
