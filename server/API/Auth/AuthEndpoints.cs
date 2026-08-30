using System.Security.Claims;
using MediatR;
using server.Application.Auth.Commands;
using server.Application.Auth.Queries;
using server.Application.Common;

namespace server.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (
            server.Application.Auth.DTOs.RegisterRequest dto,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new RegisterCommand(dto));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.Conflict(new { error = result.ErrorMessage });
        });

        group.MapPost("/login", async (
            server.Application.Auth.DTOs.LoginRequest dto,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new LoginCommand(dto));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.Unauthorized();
        });

        group.MapPost("/refresh", async (
            server.Application.Auth.DTOs.RefreshRequest dto,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new RefreshTokenCommand(dto.RefreshToken));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.Unauthorized();
        });

        group.MapPost("/logout", async (
            server.Application.Auth.DTOs.RefreshRequest dto,
            IMediator mediator) =>
        {
            await mediator.Send(new LogoutCommand(dto.RefreshToken));
            return Results.NoContent();
        });

        group.MapGet("/me", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? principal.FindFirstValue("sub");
            if (!Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();

            var result = await mediator.Send(new GetMeQuery(userId));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization();

        group.MapPost("/select-tenant", async (
            ClaimsPrincipal principal,
            SelectTenantRequest dto,
            IMediator mediator) =>
        {
            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? principal.FindFirstValue("sub");
            if (!Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();

            var role = principal.FindFirstValue("role") ?? "";
            if (role != "super_admin")
                return Results.Forbid();

            if (!Guid.TryParse(dto.TenantId, out var tenantId))
                return Results.BadRequest(new { error = "tenant_id invalide." });

            var result = await mediator.Send(new SelectTenantCommand(userId, tenantId));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization();
    }
}

public record SelectTenantRequest(string TenantId);
