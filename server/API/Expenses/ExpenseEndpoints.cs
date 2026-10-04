using System.Security.Claims;
using MediatR;
using server.Application.Common;
using server.Application.Expenses;

namespace server.API.Expenses;

public static class ExpenseEndpoints
{
    public static void MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/expenses").WithTags("Expenses").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20,
            string? status = null, string? category = null,
            string? from = null, string? to = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(
                new ListExpensesQuery(tenantId.Value, page, limit, status, category, from, to)));
        }).RequireAuthorization(Permissions.ReportsFinancial);

        group.MapGet("/stats", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetExpenseStatsQuery(tenantId.Value)));
        }).RequireAuthorization(Permissions.ReportsFinancial);

        group.MapPost("/", async (CreateExpenseRequest dto, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CreateExpenseCommand(tenantId.Value, dto));
            return result.IsSuccess
                ? Results.Created($"/api/v1/expenses/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
        }).RequireAuthorization(Permissions.ReportsFinancial);

        group.MapPut("/{id:guid}", async (Guid id, UpdateExpenseRequest dto, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new UpdateExpenseCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
        }).RequireAuthorization(Permissions.ReportsFinancial);

        group.MapPost("/{id:guid}/validate", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            var userId   = GetUserId(principal);
            if (tenantId is null || userId is null) return Results.Unauthorized();
            var result = await mediator.Send(new ValidateExpenseCommand(tenantId.Value, id, userId.Value, true));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
        }).RequireAuthorization(Permissions.ReportsFinancial);

        group.MapPost("/{id:guid}/reject", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            var userId   = GetUserId(principal);
            if (tenantId is null || userId is null) return Results.Unauthorized();
            var result = await mediator.Send(new ValidateExpenseCommand(tenantId.Value, id, userId.Value, false));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
        }).RequireAuthorization(Permissions.ReportsFinancial);

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new DeleteExpenseCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(new { deleted = true }) : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
        }).RequireAuthorization(Permissions.ReportsFinancial);
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("tenant_id"), out var id) ? id : null;

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
