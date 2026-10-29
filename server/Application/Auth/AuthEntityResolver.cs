using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;

namespace server.Application.Auth;

internal static class AuthEntityResolver
{
    internal static async Task<(string EntityType, string EntityId)> ResolveAsync(
        AppDbContext db, Guid userId, Guid? tenantId, string role, CancellationToken ct)
    {
        if (tenantId is null) return (role is "member" or "board_member" ? "user" : role, userId.ToString());
        var member = await db.Members.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId && m.IsActive, ct);
        if (member is null) return (role is "member" or "board_member" ? "user" : role, userId.ToString());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isBoardMember = await db.BoardMembers.AnyAsync(
            b => b.MemberId == member.Id && b.IsActive
              && b.StartDate <= today && (b.EndDate == null || b.EndDate >= today), ct);
        return (isBoardMember ? "board_member" : "member", member.Id.ToString());
    }
}
