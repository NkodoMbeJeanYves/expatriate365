using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Audit.DTOs;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Audit.Queries;

// ── Audit logs ────────────────────────────────────────────────────────────────

public record GetAuditLogsQuery(
    int       Page      = 1,
    int       Limit     = 20,
    string?   TenantId  = null,
    string?   UserId    = null,
    string?   Action    = null,
    DateTime? From      = null,
    DateTime? To        = null)
    : IRequest<PagedResult<AuditLogDto>>;

public class GetAuditLogsQueryHandler(AppDbContext db)
    : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery req, CancellationToken ct)
    {
        var q = db.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Tenant)
            .AsNoTracking()
            .AsQueryable();

        if (Guid.TryParse(req.TenantId, out var tid))  q = q.Where(a => a.TenantId == tid);
        if (Guid.TryParse(req.UserId,   out var uid))  q = q.Where(a => a.UserId   == uid);
        if (!string.IsNullOrEmpty(req.Action))         q = q.Where(a => a.Action   == req.Action);
        if (req.From.HasValue)                         q = q.Where(a => a.CreatedAt >= req.From.Value);
        if (req.To.HasValue)                           q = q.Where(a => a.CreatedAt <= req.To.Value);

        var total = await q.CountAsync(ct);

        var data = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((req.Page - 1) * req.Limit)
            .Take(req.Limit)
            .Select(a => new AuditLogDto(
                a.Id.ToString(),
                a.TenantId.HasValue ? a.TenantId.ToString() : null,
                a.Tenant != null ? a.Tenant.Name : null,
                a.UserId.ToString(),
                a.User != null ? a.User.FullName : "Unknown",
                a.Action,
                a.EntityType,
                a.EntityId,
                a.Meta,
                a.CreatedAt.ToString("O")))
            .ToListAsync(ct);

        return PagedResult<AuditLogDto>.Create(data, req.Page, req.Limit, total);
    }
}

// ── Tenant stats ──────────────────────────────────────────────────────────────

public record GetTenantStatsQuery : IRequest<IEnumerable<TenantStatsDto>>;

public class GetTenantStatsQueryHandler(AppDbContext db)
    : IRequestHandler<GetTenantStatsQuery, IEnumerable<TenantStatsDto>>
{
    public async Task<IEnumerable<TenantStatsDto>> Handle(GetTenantStatsQuery _, CancellationToken ct)
    {
        var tenants = await db.Tenants.AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(ct);

        var results = new List<TenantStatsDto>();

        foreach (var t in tenants)
        {
            var memberCount     = await db.Members.CountAsync(m => m.TenantId == t.Id && m.IsActive, ct);
            var postsPublished  = await db.Posts.CountAsync(p => p.TenantId == t.Id && p.IsActive && p.Status == "published", ct);
            var postsDraft      = await db.Posts.CountAsync(p => p.TenantId == t.Id && p.IsActive && p.Status == "draft",     ct);
            var postsRejected   = await db.Posts.CountAsync(p => p.TenantId == t.Id && p.IsActive && p.Status == "rejected",  ct);

            var lastLog = await db.AuditLogs
                .Where(a => a.TenantId == t.Id)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => (DateTime?)a.CreatedAt)
                .FirstOrDefaultAsync(ct);

            results.Add(new TenantStatsDto(
                t.Id.ToString(), t.Name, t.Slug,
                memberCount, postsPublished, postsDraft, postsRejected,
                lastLog.HasValue ? lastLog.Value.ToString("O") : null));
        }

        return results;
    }
}

// ── Anomalies ─────────────────────────────────────────────────────────────────

public record GetAnomaliesQuery : IRequest<IEnumerable<AnomalyDto>>;

public class GetAnomaliesQueryHandler(AppDbContext db)
    : IRequestHandler<GetAnomaliesQuery, IEnumerable<AnomalyDto>>
{
    public async Task<IEnumerable<AnomalyDto>> Handle(GetAnomaliesQuery _, CancellationToken ct)
    {
        var anomalies = new List<AnomalyDto>();
        var now = DateTime.UtcNow;

        // Posts stuck in draft for more than 7 days
        var staleDraftCutoff = now.AddDays(-7);
        var staleDrafts = await db.Posts
            .Include(p => p.Author).ThenInclude(m => m.Tenant)
            .AsNoTracking()
            .Where(p => p.IsActive && p.Status == "draft" && p.CreatedAt < staleDraftCutoff)
            .ToListAsync(ct);

        foreach (var p in staleDrafts)
        {
            var tenantName = p.Author?.Tenant?.Name;
            anomalies.Add(new AnomalyDto(
                "pending_post",
                $"Post \"{p.Title}\" pending moderation for {(int)(now - p.CreatedAt).TotalDays} days",
                "warning",
                p.TenantId.ToString(),
                tenantName,
                p.Id.ToString()));
        }

        // Tenants with no published post in last 30 days
        var inactiveCutoff = now.AddDays(-30);
        var activeTenants  = await db.Tenants.AsNoTracking().Where(t => t.IsActive).ToListAsync(ct);

        foreach (var t in activeTenants)
        {
            var hasRecentPost = await db.Posts.AnyAsync(
                p => p.TenantId == t.Id && p.Status == "published" && p.PublishedAt >= inactiveCutoff, ct);

            if (!hasRecentPost)
            {
                anomalies.Add(new AnomalyDto(
                    "inactive_tenant",
                    $"Community \"{t.Name}\" has no published post in the last 30 days",
                    "info",
                    t.Id.ToString(),
                    t.Name,
                    null));
            }
        }

        // Members expiring within 7 days
        var expiryWarn = DateOnly.FromDateTime(now.AddDays(7));
        var expiring = await db.Members
            .Include(m => m.Tenant)
            .AsNoTracking()
            .Where(m => m.IsActive && m.ExpiryDate.HasValue && m.ExpiryDate <= expiryWarn)
            .Take(50)
            .ToListAsync(ct);

        foreach (var m in expiring)
        {
            anomalies.Add(new AnomalyDto(
                "expiring_member",
                $"Member #{m.MembershipNumber} membership expires on {m.ExpiryDate}",
                "warning",
                m.TenantId.ToString(),
                m.Tenant?.Name,
                m.Id.ToString()));
        }

        return anomalies.OrderBy(a => a.Severity == "critical" ? 0 : a.Severity == "warning" ? 1 : 2);
    }
}
