using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.API.Notifications;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications").WithTags("Notifications").RequireAuthorization();

        // GET my notifications
        group.MapGet("/", async (ClaimsPrincipal principal, AppDbContext db,
            int page = 1, int limit = 20, bool? unread_only = null) =>
        {
            var userId = GetUserId(principal);
            var query = db.Notifications
                .Where(n => n.UserId == userId && n.IsActive)
                .AsQueryable();

            if (unread_only == true) query = query.Where(n => !n.IsRead);

            var total = await query.CountAsync();
            var unreadCount = await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead && n.IsActive);
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * limit).Take(limit)
                .Select(n => new
                {
                    n.Id,
                    n.Type,
                    n.Title,
                    n.Body,
                    n.IsRead,
                    n.ReadAt,
                    n.CreatedAt,
                })
                .ToListAsync();

            return Results.Ok(new { data = items, unread_count = unreadCount, pagination = new { page, limit, total } });
        }).RequireAuthorization(Permissions.NotificationsReadOwn);

        // Mark one as read
        group.MapPost("/{id:guid}/read", async (Guid id, ClaimsPrincipal principal, AppDbContext db) =>
        {
            var userId = GetUserId(principal);
            var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (n is null) return Results.NotFound(new { error = "Notification introuvable." });
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        }).RequireAuthorization(Permissions.NotificationsReadOwn);

        // Mark all as read
        group.MapPost("/read-all", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var userId = GetUserId(principal);
            var unread = await db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead && n.IsActive)
                .ToListAsync();
            var now = DateTime.UtcNow;
            foreach (var n in unread) { n.IsRead = true; n.ReadAt = now; }
            await db.SaveChangesAsync();
            return Results.Ok(new { marked = unread.Count });
        }).RequireAuthorization(Permissions.NotificationsReadOwn);
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }
}
