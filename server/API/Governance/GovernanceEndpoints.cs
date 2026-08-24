using System.Security.Claims;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Governance.Commands;
using server.Application.Governance.DTOs;
using server.Application.Governance.Queries;
using server.Infrastructure.Persistence;

namespace server.API.Governance;

public static class GovernanceEndpoints
{
    public static void MapGovernanceEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/governance").WithTags("Governance").RequireAuthorization();

        grp.MapGet("/stats", async (ClaimsPrincipal p, IMediator m) =>
        {
            var tid = GetTenantId(p); if (tid is null) return Results.Unauthorized();
            return Results.Ok(await m.Send(new GetGovernanceStatsQuery(tid.Value)));
        }).RequireAuthorization(Permissions.DashboardRead);

        // Board
        grp.MapGet("/board", async (ClaimsPrincipal p, IMediator m) =>
        {
            var tid = GetTenantId(p); if (tid is null) return Results.Unauthorized();
            return Results.Ok(await m.Send(new ListBoardMembersQuery(tid.Value)));
        }).RequireAuthorization(Permissions.RolesRead);

        grp.MapPost("/board", async (CreateBoardMemberRequest req, ClaimsPrincipal p, IMediator m) =>
        {
            var tid = GetTenantId(p); if (tid is null) return Results.Unauthorized();
            var result = await m.Send(new CreateBoardMemberCommand(tid.Value, req));
            return result.IsSuccess
                ? Results.Created($"/api/v1/governance/board/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.RolesAssign);

        grp.MapGet("/board/me", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var tid = GetTenantId(p); if (tid is null) return Results.Unauthorized();
            var userId = GetUserId(p); if (userId == Guid.Empty) return Results.Unauthorized();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tid.Value && m.IsActive);
            var isBoardMember = member is not null && await db.BoardMembers.AnyAsync(
                b => b.MemberId == member.Id && b.IsActive && b.StartDate <= today && (b.EndDate == null || b.EndDate >= today));
            return Results.Ok(new { is_board_member = isBoardMember });
        }).RequireAuthorization();

        grp.MapDelete("/board/{id:guid}", async (Guid id, ClaimsPrincipal p, IMediator m) =>
        {
            var tid = GetTenantId(p); if (tid is null) return Results.Unauthorized();
            var result = await m.Send(new DeleteBoardMemberCommand(tid.Value, id));
            return result.IsSuccess ? Results.NoContent() : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.RolesAssign);

        // Resolutions
        grp.MapGet("/resolutions", async (ClaimsPrincipal p, IMediator m,
            int page = 1, int limit = 20, string? status = null) =>
        {
            var tid = GetTenantId(p); if (tid is null) return Results.Unauthorized();
            return Results.Ok(await m.Send(new ListResolutionsQuery(tid.Value, page, limit, status)));
        }).RequireAuthorization(Permissions.ReportsRead);

        grp.MapPost("/resolutions", async (CreateResolutionRequest req, ClaimsPrincipal p, IMediator m) =>
        {
            var tid = GetTenantId(p); if (tid is null) return Results.Unauthorized();
            var result = await m.Send(new CreateResolutionCommand(tid.Value, req));
            return result.IsSuccess
                ? Results.Created($"/api/v1/governance/resolutions/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.RolesUpdate);

        grp.MapPost("/resolutions/{id:guid}/adopt", async (Guid id, AdoptResolutionRequest req, ClaimsPrincipal p, IMediator m) =>
        {
            var tid = GetTenantId(p); if (tid is null) return Results.Unauthorized();
            var result = await m.Send(new AdoptResolutionCommand(tid.Value, id, req));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.RolesUpdate);

        grp.MapDelete("/resolutions/{id:guid}", async (Guid id, ClaimsPrincipal p, IMediator m) =>
        {
            var tid = GetTenantId(p); if (tid is null) return Results.Unauthorized();
            var result = await m.Send(new DeleteResolutionCommand(tid.Value, id));
            return result.IsSuccess ? Results.NoContent() : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.RolesDelete);
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }
}
