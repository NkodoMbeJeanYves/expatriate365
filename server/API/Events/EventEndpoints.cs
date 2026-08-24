using System.Security.Claims;
using MediatR;
using server.Application.Common;
using server.Application.Events.Commands;
using server.Application.Events.DTOs;
using server.Application.Events.Queries;

namespace server.API.Events;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/events").WithTags("Events").RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20, string? status = null, string? type = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new ListEventsQuery(tenantId.Value, page, limit, status, type));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.EventsRead);

        group.MapGet("/stats", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetEventStatsQuery(tenantId.Value)));
        }).RequireAuthorization(Permissions.EventsRead);

        group.MapPost("/", async (ClaimsPrincipal principal, IMediator mediator, CreateEventRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CreateEventCommand(tenantId.Value, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsCreate);

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new GetEventByIdQuery(tenantId.Value, id));
            return result.IsSuccess
                ? Results.Ok(new { @event = result.Data.Event, registrations = result.Data.Registrations })
                : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsRead);

        group.MapPut("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator, UpdateEventRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new UpdateEventCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsUpdate);

        group.MapPost("/{id:guid}/publish", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new PublishEventCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsUpdate);

        group.MapPost("/{id:guid}/complete", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CompleteEventCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsUpdate);

        group.MapPost("/{id:guid}/cancel-event", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CancelEventCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsDelete);

        group.MapGet("/{id:guid}/registrations", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new GetEventByIdQuery(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data.Registrations) : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsRead);

        group.MapPost("/{id:guid}/registrations", async (Guid id, ClaimsPrincipal principal, IMediator mediator, RegisterToEventRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new RegisterToEventCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsManageAttendees);

        group.MapDelete("/{id:guid}/registrations/{regId:guid}", async (Guid id, Guid regId, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CancelRegistrationCommand(tenantId.Value, id, regId));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsManageAttendees);

        group.MapPost("/{id:guid}/attendance", async (Guid id, ClaimsPrincipal principal, IMediator mediator, MarkAttendanceRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new MarkAttendanceCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(new { updated = result.Data }) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.EventsManageAttendees);
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("tenant_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
