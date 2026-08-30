using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Community.Queries;
using server.Infrastructure.Persistence;

namespace server.API.Community;

public static class ExploreEndpoints
{
    public static void MapExploreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/explore").WithTags("Explore");

        // Public feed — no auth required; tenant resolved by slug
        group.MapGet("/{slug}/posts", async (
            string slug, AppDbContext db, IMediator mediator,
            int page = 1, int limit = 12, string? search = null) =>
        {
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == slug && t.IsActive);
            if (tenant is null)
                return Results.NotFound(new { error = "Communauté introuvable." });

            var result = await mediator.Send(
                new ListPostsQuery(tenant.Id, page, limit, "published", null, search));
            return Results.Ok(result);
        });

        // Public single post
        group.MapGet("/{slug}/posts/{id:guid}", async (
            string slug, Guid id, AppDbContext db, IMediator mediator) =>
        {
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == slug && t.IsActive);
            if (tenant is null)
                return Results.NotFound(new { error = "Communauté introuvable." });

            var post = await mediator.Send(new GetPostQuery(tenant.Id, id));
            if (post is null || post.Status != "published")
                return Results.NotFound(new { error = "Publication introuvable." });

            return Results.Ok(post);
        });
    }
}
