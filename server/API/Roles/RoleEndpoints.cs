using System.Security.Claims;
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

        // GET /api/v1/roles — global role templates (super_admin only)
        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListRolesQuery())))
            .RequireAuthorization(Permissions.RolesUpdate)
            .WithName("ListRoles");

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
        .RequireAuthorization(Permissions.RolesRead);

        // GET /api/v1/roles/tenant — tenant-specific role list (any admin)
        group.MapGet("/tenant", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new ListTenantRolesQuery(tenantId.Value)));
        })
        .RequireAuthorization(Permissions.RolesRead)
        .WithName("ListTenantRoles");

        // PUT /api/v1/roles/tenant/{id}/permissions — update tenant-specific permissions
        group.MapPut("/tenant/{id:guid}/permissions",
            async (Guid id, UpdateRolePermissionsRequest dto, ClaimsPrincipal principal, IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                if (tenantId is null) return Results.Unauthorized();
                var callerRole = principal.FindFirstValue("role") ?? "";
                var result = await mediator.Send(new UpdateTenantRolePermissionsCommand(tenantId.Value, id, dto, callerRole));
                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.RolesUpdate)
            .WithName("UpdateTenantRolePermissions");

        // POST /api/v1/roles/tenant/{id}/reset — restore global template permissions
        group.MapPost("/tenant/{id:guid}/reset",
            async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                if (tenantId is null) return Results.Unauthorized();
                var result = await mediator.Send(new ResetTenantRolePermissionsCommand(tenantId.Value, id));
                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.RolesUpdate)
            .WithName("ResetTenantRolePermissions");

        // PUT /api/v1/roles/{id}/permissions — update global template (super_admin only)
        group.MapPut("/{id:guid}/permissions",
            async (Guid id, UpdateRolePermissionsRequest dto, IMediator mediator) =>
            {
                var result = await mediator.Send(new UpdateRolePermissionsCommand(id, dto));
                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.RolesUpdate)
            .WithName("UpdateRolePermissions");

        // POST /api/v1/roles/{id}/reset — restore seeder default permissions (super_admin only)
        group.MapPost("/{id:guid}/reset",
            async (Guid id, IMediator mediator) =>
            {
                var result = await mediator.Send(new ResetRolePermissionsCommand(id));
                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.RolesUpdate)
            .WithName("ResetRolePermissions");
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
