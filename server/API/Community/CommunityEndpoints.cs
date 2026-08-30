using System.Security.Claims;
using MediatR;
using server.Application.Common;
using server.Application.Community.Commands;
using server.Application.Community.DTOs;
using server.Application.Community.Queries;

namespace server.API.Community;

public static class CommunityEndpoints
{
    public static void MapCommunityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/posts").WithTags("Community").RequireAuthorization();

        // List posts — published feed (all members) or filtered (staff)
        group.MapGet("/", async (ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20, string? status = null,
            string? author_id = null, string? search = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();

            // Non-staff members can only see published posts
            var isStaff = IsStaff(principal);
            var effectiveStatus = isStaff ? status : "published";

            return Results.Ok(await mediator.Send(
                new ListPostsQuery(tenantId.Value, page, limit, effectiveStatus, author_id, search)));
        }).RequireAuthorization(Permissions.CommunityRead);

        // Get single post
        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();

            var post = await mediator.Send(new GetPostQuery(tenantId.Value, id));
            if (post is null) return Results.NotFound(new { error = "Publication introuvable." });

            var isStaff = IsStaff(principal);
            var memberId = GetMemberId(principal);
            if (post.Status != "published" && !isStaff && post.AuthorId != memberId?.ToString())
                return Results.Forbid();

            return Results.Ok(post);
        }).RequireAuthorization(Permissions.CommunityRead);

        // Create draft
        group.MapPost("/", async (CreatePostRequest request, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            var memberId = GetMemberId(principal);
            var userId   = GetUserId(principal);
            if (tenantId is null || userId is null) return Results.Unauthorized();

            var result = await mediator.Send(new CreatePostCommand(tenantId.Value, memberId ?? userId.Value, userId.Value, request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/posts/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.CommunityWrite);

        // Update draft
        group.MapPut("/{id:guid}", async (Guid id, UpdatePostRequest request,
            ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            var memberId = GetMemberId(principal);
            if (tenantId is null || memberId is null) return Results.Unauthorized();

            var result = await mediator.Send(new UpdatePostCommand(tenantId.Value, id, memberId.Value, request));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.CommunityWrite);

        // Soft-delete
        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            var memberId = GetMemberId(principal);
            if (tenantId is null || memberId is null) return Results.Unauthorized();

            var isStaff = IsStaff(principal);
            var result = await mediator.Send(new DeletePostCommand(tenantId.Value, id, memberId.Value, isStaff));
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.CommunityWrite);

        // Publish (staff only)
        group.MapPost("/{id:guid}/publish", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();

            var result = await mediator.Send(new PublishPostCommand(tenantId.Value, id));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.CommunityModerate);

        // Reject (staff only)
        group.MapPost("/{id:guid}/reject", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();

            var result = await mediator.Send(new RejectPostCommand(tenantId.Value, id));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.CommunityModerate);

        // Add attachment
        group.MapPost("/{id:guid}/attachments", async (Guid id, AddAttachmentRequest request,
            ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            var memberId = GetMemberId(principal);
            if (tenantId is null || memberId is null) return Results.Unauthorized();

            var result = await mediator.Send(new AddAttachmentCommand(tenantId.Value, id, memberId.Value, request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/posts/{id}/attachments/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.CommunityWrite);

        // Delete attachment
        group.MapDelete("/{id:guid}/attachments/{attachmentId:guid}", async (
            Guid id, Guid attachmentId, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            var memberId = GetMemberId(principal);
            if (tenantId is null || memberId is null) return Results.Unauthorized();

            var result = await mediator.Send(new DeleteAttachmentCommand(tenantId.Value, id, attachmentId, memberId.Value));
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.CommunityWrite);
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

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirstValue("sub")
                 ?? principal.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static bool IsStaff(ClaimsPrincipal principal)
    {
        var role = principal.FindFirstValue("role")
                ?? principal.FindFirstValue(System.Security.Claims.ClaimTypes.Role)
                ?? "";
        // Only plain members are restricted to published posts
        return role != "member";
    }
}
