using System.Security.Claims;
using MediatR;
using server.Application.Common;
using server.Application.Meetings.Commands;
using server.Application.Meetings.DTOs;
using server.Application.Meetings.Queries;

namespace server.API.Meetings;

public static class MeetingEndpoints
{
    public static void MapMeetingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/meetings").WithTags("Meetings").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20, string? status = null, string? type = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new ListMeetingsQuery(tenantId.Value, page, limit, status, type)));
        }).RequireAuthorization(Permissions.EventsRead);

        group.MapGet("/stats", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetMeetingStatsQuery(tenantId.Value)));
        }).RequireAuthorization(Permissions.EventsRead);

        group.MapPost("/", async (ClaimsPrincipal principal, IMediator mediator, CreateMeetingRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CreateMeetingCommand(tenantId.Value, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsCreate);

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new GetMeetingByIdQuery(tenantId.Value, id));
            return result.IsSuccess
                ? Results.Ok(new { meeting = result.Data.Meeting, attendances = result.Data.Attendances, minute = result.Data.Minute })
                : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsRead);

        group.MapPut("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator, UpdateMeetingRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new UpdateMeetingCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsUpdate);

        group.MapPost("/{id:guid}/start", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new StartMeetingCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsUpdate);

        group.MapPost("/{id:guid}/close", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CloseMeetingCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsUpdate);

        group.MapPost("/{id:guid}/cancel", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CancelMeetingCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsDelete);

        group.MapPost("/{id:guid}/attendance", async (Guid id, ClaimsPrincipal principal, IMediator mediator, RecordAttendanceRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new RecordAttendanceCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(new { updated = result.Data }) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsManageAttendees);

        group.MapPut("/{id:guid}/minutes", async (Guid id, ClaimsPrincipal principal, IMediator mediator, SaveMinutesRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new SaveMinutesCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsUpdate);

        group.MapPost("/{id:guid}/minutes/approve", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new ApproveMinutesCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsUpdate);

        // ── Action Items ─────────────────────────────────────────────────────

        group.MapGet("/{id:guid}/action-items", async (
            Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new ListActionItemsQuery(tenantId.Value, id));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.EventsRead);

        group.MapPost("/{id:guid}/action-items", async (
            Guid id, ClaimsPrincipal principal, IMediator mediator, CreateActionItemRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CreateActionItemCommand(tenantId.Value, id, dto));
            return result.IsSuccess
                ? Results.Created($"/api/v1/meetings/{id}/action-items/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsUpdate);

        group.MapPut("/action-items/{itemId:guid}", async (
            Guid itemId, ClaimsPrincipal principal, IMediator mediator, UpdateActionItemRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new UpdateActionItemCommand(tenantId.Value, itemId, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsUpdate);

        group.MapDelete("/action-items/{itemId:guid}", async (
            Guid itemId, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new DeleteActionItemCommand(tenantId.Value, itemId));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsUpdate);
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("tenant_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
