using System.Security.Claims;
using MediatR;
using server.Application.Admin.Commands;
using server.Application.Admin.DTOs;
using server.Application.Admin.Queries;
using server.Application.Common;

namespace server.API.Admin;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin").WithTags("Admin").RequireAuthorization();

        group.MapGet("/stats", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetAdminStatsQuery(tenantId.Value)));
        }).RequireAuthorization(Permissions.DashboardMembers);

        group.MapGet("/users", async (ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20, string? role = null, string? status = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new ListAdminUsersQuery(tenantId.Value, page, limit, role, status)));
        }).RequireAuthorization(Permissions.UsersRead);

        group.MapPost("/users/invite", async (InviteUserRequest request, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new InviteUserCommand(tenantId.Value, request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/admin/users/{result.Data!.Id}", result.Data)
                : Results.Conflict(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.UsersCreate);

        group.MapPut("/users/{id:guid}/role", async (Guid id, ChangeRoleRequest request,
            ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new ChangeUserRoleCommand(tenantId.Value, id, request));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.RolesAssign);

        group.MapPost("/users/{id:guid}/activate", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new ToggleUserStatusCommand(tenantId.Value, id, true));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.UsersUpdate);

        group.MapPost("/users/{id:guid}/deactivate", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new ToggleUserStatusCommand(tenantId.Value, id, false));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.UsersUpdate);
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
