using System.Security.Claims;
using MediatR;
using server.Application.Common;
using server.Application.Finances.Queries;

namespace server.API.Finances;

public static class FinanceEndpoints
{
    public static void MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/finances").WithTags("Finances").RequireAuthorization();

        group.MapGet("/summary", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetFinanceSummaryQuery(tenantId.Value)));
        }).RequireAuthorization(Permissions.ReportsFinancial);

        group.MapGet("/transactions", async (ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 25, string? type = null,
            string? status = null, string? from = null, string? to = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new ListFinanceTransactionsQuery(
                tenantId.Value, page, limit, type, status, from, to)));
        }).RequireAuthorization(Permissions.ReportsFinancial);
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
