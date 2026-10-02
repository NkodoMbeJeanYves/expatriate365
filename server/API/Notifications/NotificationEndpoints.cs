using System.Security.Claims;
using MediatR;
using server.Application.Notifications;

namespace server.API.Notifications;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20, bool? unread = null) =>
        {
            var (tenantId, userId) = GetIds(principal);
            if (tenantId is null || userId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new ListNotificationsQuery(tenantId.Value, userId.Value, page, limit, unread)));
        });

        group.MapPatch("/{id:guid}/read", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var (tenantId, userId) = GetIds(principal);
            if (tenantId is null || userId is null) return Results.Unauthorized();
            var result = await mediator.Send(new MarkNotificationReadCommand(tenantId.Value, userId.Value, id));
            return result.IsSuccess ? Results.Ok(new { ok = true }) : Results.NotFound(new { error = result.ErrorMessage });
        });

        group.MapPost("/read-all", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var (tenantId, userId) = GetIds(principal);
            if (tenantId is null || userId is null) return Results.Unauthorized();
            var result = await mediator.Send(new MarkAllNotificationsReadCommand(tenantId.Value, userId.Value));
            return Results.Ok(new { marked = result.Data });
        });

        group.MapGet("/preferences", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var (tenantId, userId) = GetIds(principal);
            if (tenantId is null || userId is null) return Results.Unauthorized();
            var result = await mediator.Send(new GetNotificationPreferencesQuery(tenantId.Value, userId.Value));
            return Results.Ok(result);
        });

        group.MapPut("/preferences", async (UpdatePreferencesRequest dto, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var (tenantId, userId) = GetIds(principal);
            if (tenantId is null || userId is null) return Results.Unauthorized();
            var result = await mediator.Send(new UpdateNotificationPreferencesCommand(tenantId.Value, userId.Value, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        });
    }

    private static (Guid? tenantId, Guid? userId) GetIds(ClaimsPrincipal principal)
    {
        Guid? tenantId = Guid.TryParse(principal.FindFirstValue("tenant_id"), out var tid) ? tid : null;
        Guid? userId   = Guid.TryParse(principal.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out var uid) ? uid : null;
        return (tenantId, userId);
    }
}
