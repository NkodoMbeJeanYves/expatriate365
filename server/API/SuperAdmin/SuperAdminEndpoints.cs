using MediatR;
using Microsoft.AspNetCore.Mvc;
using server.Application.SuperAdmin;

namespace server.API.SuperAdmin;

public static class SuperAdminEndpoints
{
    public static void MapSuperAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/superadmin")
            .WithTags("SuperAdmin")
            .RequireAuthorization();

        group.MapGet("/tenants", async (HttpContext ctx, IMediator mediator) =>
        {
            if (!IsSuperAdmin(ctx)) return Results.Forbid();
            var result = await mediator.Send(new ListTenantsQuery());
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("ListTenants")
        .WithSummary("List all associations (super_admin only)");

        group.MapPost("/tenants", async (HttpContext ctx, [FromBody] CreateTenantRequest dto, IMediator mediator) =>
        {
            if (!IsSuperAdmin(ctx)) return Results.Forbid();
            var result = await mediator.Send(new CreateTenantCommand(dto));
            return result.IsSuccess ? Results.Created($"/api/v1/superadmin/tenants", result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("CreateTenant")
        .WithSummary("Create a new association with its org_admin (super_admin only)");
    }

    private static bool IsSuperAdmin(HttpContext ctx)
    {
        var role = ctx.User.FindFirst("role")?.Value
                ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        return role == "super_admin";
    }
}
