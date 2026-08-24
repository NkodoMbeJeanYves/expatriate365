using System.Security.Claims;
using MediatR;
using server.Application.Common;
using server.Application.Communications.Commands;
using server.Application.Communications.DTOs;
using server.Application.Communications.Queries;

namespace server.API.Communications;

public static class CommunicationEndpoints
{
    public static void MapCommunicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/communications").WithTags("Communications").RequireAuthorization();

        group.MapGet("/stats", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetCommunicationStatsQuery(tenantId.Value)));
        }).RequireAuthorization(Permissions.AnnouncementsRead);

        group.MapGet("/", async (ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20, string? status = null, string? type = null, string? channel = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new ListCommunicationsQuery(tenantId.Value, page, limit, status, type, channel)));
        }).RequireAuthorization(Permissions.AnnouncementsRead);

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new GetCommunicationByIdQuery(tenantId.Value, id));
            return result.IsSuccess
                ? Results.Ok(new { communication = result.Data.Communication, recipients = result.Data.Recipients })
                : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.AnnouncementsRead);

        group.MapPost("/", async (CreateCommunicationRequest request, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CreateCommunicationCommand(tenantId.Value, request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/communications/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.AnnouncementsCreate);

        group.MapPut("/{id:guid}", async (Guid id, UpdateCommunicationRequest request,
            ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new UpdateCommunicationCommand(tenantId.Value, id, request));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.AnnouncementsUpdate);

        group.MapPost("/{id:guid}/send", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new SendCommunicationCommand(tenantId.Value, id));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.AnnouncementsPublish);

        group.MapPost("/{id:guid}/read", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            var memberId = GetMemberId(principal);
            if (tenantId is null || memberId is null) return Results.Unauthorized();
            var result = await mediator.Send(new MarkReadCommand(tenantId.Value, id, memberId.Value));
            return result.IsSuccess
                ? Results.Ok()
                : Results.BadRequest(new { error = result.ErrorMessage });
        });
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static Guid? GetMemberId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("entity_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
