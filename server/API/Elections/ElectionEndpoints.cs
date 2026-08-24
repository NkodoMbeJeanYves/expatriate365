using System.Security.Claims;
using MediatR;
using server.Application.Elections.Commands;
using server.Application.Elections.DTOs;
using server.Application.Elections.Queries;

namespace server.API.Elections;

public static class ElectionEndpoints
{
    public static void MapElectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/elections").WithTags("Elections").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20, string? status = null, string? type = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new ListElectionsQuery(tenantId.Value, page, limit, status, type)));
        });

        group.MapGet("/stats", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetElectionStatsQuery(tenantId.Value)));
        });

        group.MapPost("/", async (ClaimsPrincipal principal, IMediator mediator, CreateElectionRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CreateElectionCommand(tenantId.Value, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new GetElectionByIdQuery(tenantId.Value, id));
            return result.IsSuccess
                ? Results.Ok(new { election = result.Data.Election, candidates = result.Data.Candidates, results = result.Data.Results })
                : Results.NotFound(new { error = result.ErrorMessage });
        });

        group.MapPut("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator, UpdateElectionRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new UpdateElectionCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        });

        group.MapPost("/{id:guid}/open", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new OpenElectionCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        });

        group.MapPost("/{id:guid}/close", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CloseElectionCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        });

        group.MapPost("/{id:guid}/publish-results", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new PublishResultsCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        });

        group.MapPost("/{id:guid}/candidates", async (Guid id, ClaimsPrincipal principal, IMediator mediator, AddCandidateRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new AddCandidateCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        });

        group.MapDelete("/{id:guid}/candidates/{candidateId:guid}", async (Guid id, Guid candidateId, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new RemoveCandidateCommand(tenantId.Value, id, candidateId));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(new { error = result.ErrorMessage });
        });

        group.MapPost("/{id:guid}/vote", async (Guid id, ClaimsPrincipal principal, IMediator mediator, CastVoteRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            var voterId = GetUserId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CastVoteCommand(tenantId.Value, id, voterId, dto));
            return result.IsSuccess ? Results.Ok(new { voted = true }) : Results.BadRequest(new { error = result.ErrorMessage });
        });
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
