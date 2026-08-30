using System.Security.Claims;
using MediatR;
using server.Application.Audit.Queries;
using server.Application.Common;

namespace server.API.Audit;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/audit").WithTags("Audit").RequireAuthorization();

        group.MapGet("/logs", async (ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20,
            string? tenant_id = null, string? user_id = null, string? action = null,
            DateTime? from = null, DateTime? to = null) =>
        {
            if (!IsSuperAdmin(principal)) return Results.Forbid();
            return Results.Ok(await mediator.Send(
                new GetAuditLogsQuery(page, limit, tenant_id, user_id, action, from, to)));
        }).RequireAuthorization(Permissions.AuditRead);

        group.MapGet("/tenants", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            if (!IsSuperAdmin(principal)) return Results.Forbid();
            return Results.Ok(await mediator.Send(new GetTenantStatsQuery()));
        }).RequireAuthorization(Permissions.AuditRead);

        group.MapGet("/anomalies", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            if (!IsSuperAdmin(principal)) return Results.Forbid();
            return Results.Ok(await mediator.Send(new GetAnomaliesQuery()));
        }).RequireAuthorization(Permissions.AuditRead);
    }

    private static bool IsSuperAdmin(ClaimsPrincipal principal)
    {
        var role = principal.FindFirstValue("role")
                ?? principal.FindFirstValue(ClaimTypes.Role) ?? "";
        return role == "super_admin";
    }
}
