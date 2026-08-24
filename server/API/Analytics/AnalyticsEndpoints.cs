using System.Security.Claims;
using MediatR;
using server.Application.Common;
using server.Application.Analytics.Queries;

namespace server.API.Analytics;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/analytics").WithTags("Analytics").RequireAuthorization();

        group.MapGet("/overview", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetAnalyticsOverviewQuery(tenantId.Value)));
        }).RequireAuthorization(Permissions.DashboardRead);

        group.MapGet("/members", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetMemberAnalyticsQuery(tenantId.Value)));
        }).RequireAuthorization(Permissions.DashboardMembers);

        group.MapGet("/finance", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetFinanceAnalyticsQuery(tenantId.Value)));
        }).RequireAuthorization(Permissions.DashboardFinancial);

        group.MapGet("/engagement", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetEngagementAnalyticsQuery(tenantId.Value)));
        }).RequireAuthorization(Permissions.DashboardRead);
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
