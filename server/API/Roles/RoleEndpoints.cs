using MediatR;
using server.Application.Common;
using server.Application.Roles.Commands;
using server.Application.Roles.Queries;

namespace server.API.Roles;

public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/roles").WithTags("Roles").RequireAuthorization();

        // GET /api/v1/roles — list all active roles with their permissions
        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListRolesQuery())))
            .WithName("ListRoles")
            .WithOpenApi();

        // GET /api/v1/roles/permissions — full permission catalogue grouped by domain
        group.MapGet("/permissions", () =>
        {
            var result = Permissions.ByDomain.Select(kvp => new
            {
                domain      = kvp.Key,
                permissions = kvp.Value,
            });
            return Results.Ok(result);
        })
        .WithName("ListPermissions")
        .WithOpenApi();

        // PUT /api/v1/roles/{id}/permissions — update role permissions (super_admin only)
        group.MapPut("/{id:guid}/permissions",
            async (Guid id, UpdateRolePermissionsRequest dto, IMediator mediator) =>
            {
                var result = await mediator.Send(new UpdateRolePermissionsCommand(id, dto));
                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.BadRequest(new { error = result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.RolesUpdate)
            .WithName("UpdateRolePermissions")
            .WithOpenApi();

        // POST /api/v1/roles/{id}/reset — restore seeder default permissions
        group.MapPost("/{id:guid}/reset",
            async (Guid id, IMediator mediator) =>
            {
                var result = await mediator.Send(new ResetRolePermissionsCommand(id));
                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.BadRequest(new { error = result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.RolesUpdate)
            .WithName("ResetRolePermissions")
            .WithOpenApi();
    }
}
