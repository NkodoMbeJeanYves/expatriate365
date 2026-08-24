using System.Security.Claims;
using MediatR;
using server.Application.Contributions.Commands;
using server.Application.Contributions.DTOs;
using server.Application.Contributions.Queries;

namespace server.API.Contributions;

public static class ContributionEndpoints
{
    public static void MapContributionEndpoints(this IEndpointRouteBuilder app)
    {
        var plans = app.MapGroup("/api/v1/contribution-types").WithTags("Contributions").RequireAuthorization();
        var charges = app.MapGroup("/api/v1/contribution-charges").WithTags("Contributions").RequireAuthorization();

        // --- Plans (ContributionType) ---

        plans.MapGet("/", async (ClaimsPrincipal principal, IMediator mediator, bool? is_active = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new ListContributionTypesQuery(tenantId.Value, is_active));
            return Results.Ok(result);
        });

        plans.MapPost("/", async (ClaimsPrincipal principal, IMediator mediator, CreateContributionTypeRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CreateContributionTypeCommand(tenantId.Value, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        });

        plans.MapPut("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator, UpdateContributionTypeRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new UpdateContributionTypeCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        });

        // --- Charges (ContributionCharge) ---

        charges.MapGet("/", async (
            ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20,
            string? member_id = null, string? type_id = null, string? status = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            // If caller is a member, force-scope to their own entity_id regardless of query param
            var entityType = principal.FindFirstValue("entity_type");
            var entityId   = principal.FindFirstValue("entity_id");
            if (entityType == "member" && !string.IsNullOrWhiteSpace(entityId))
                member_id = entityId;
            var result = await mediator.Send(new ListContributionChargesQuery(tenantId.Value, page, limit, member_id, type_id, status));
            return Results.Ok(result);
        });

        charges.MapGet("/stats", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new GetContributionStatsQuery(tenantId.Value));
            return Results.Ok(result);
        });

        charges.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new GetContributionChargeByIdQuery(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.NotFound(new { error = result.ErrorMessage });
        });

        charges.MapPost("/", async (ClaimsPrincipal principal, IMediator mediator, CreateContributionChargeRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CreateContributionChargeCommand(tenantId.Value, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        });

        charges.MapPost("/{id:guid}/pay", async (Guid id, ClaimsPrincipal principal, IMediator mediator, MarkChargePaidRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new MarkChargePaidCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        });

        charges.MapPost("/{id:guid}/waive", async (Guid id, ClaimsPrincipal principal, IMediator mediator, WaiveChargeRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new WaiveChargeCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        });

        charges.MapPost("/bulk-generate", async (ClaimsPrincipal principal, IMediator mediator, BulkGenerateRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new BulkGenerateChargesCommand(tenantId.Value, dto));
            return result.IsSuccess ? Results.Ok(new { generated = result.Data }) : Results.BadRequest(new { error = result.ErrorMessage });
        });
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("tenant_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
