using System.Security.Claims;
using MediatR;
using server.Application.Common;
using server.Application.Welfare.Commands;
using server.Application.Welfare.DTOs;
using server.Application.Welfare.Queries;

namespace server.API.Welfare;

public static class WelfareEndpoints
{
    public static void MapWelfareEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/welfare-requests").WithTags("Welfare").RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20,
            string? member_id = null, string? status = null, string? type = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new ListWelfareRequestsQuery(tenantId.Value, page, limit, member_id, status, type));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.ContributionsRead);

        group.MapGet("/stats", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetWelfareStatsQuery(tenantId.Value)));
        }).RequireAuthorization(Permissions.ContributionsRead);

        group.MapPost("/", async (ClaimsPrincipal principal, IMediator mediator, CreateWelfareRequestRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CreateWelfareRequestCommand(tenantId.Value, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.ContributionsCreate);

        group.MapPost("/{id:guid}/approve", async (Guid id, ClaimsPrincipal principal, IMediator mediator, ApproveWelfareRequestRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new ApproveWelfareRequestCommand(tenantId.Value, id, GetUserId(principal), dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.ContributionsValidate);

        group.MapPost("/{id:guid}/reject", async (Guid id, ClaimsPrincipal principal, IMediator mediator, RejectWelfareRequestRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new RejectWelfareRequestCommand(tenantId.Value, id, GetUserId(principal), dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.ContributionsValidate);

        group.MapPost("/{id:guid}/pay", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new MarkWelfarePaidCommand(tenantId.Value, id, GetUserId(principal)));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.ContributionsValidate);
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("tenant_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }
}
