using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using server.Application.Common;
using server.Application.TransferAssistance.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.TransferAssistance.Queries;

public record GetTransferAssistanceRulesQuery(Guid TenantId, Guid MemberId)
    : IRequest<TransferAssistanceRulesResponse>;

public class GetTransferAssistanceRulesHandler(
    AppDbContext db,
    IOptions<TransferAssistanceOptions> options,
    ILogger<GetTransferAssistanceRulesHandler> log)
    : IRequestHandler<GetTransferAssistanceRulesQuery, TransferAssistanceRulesResponse>
{
    public const string AmountCurrency = "MUR";
    private const string CommunityAdminRole = "org_admin";
    private const string PlatformAdminRole = "super_admin";

    public async Task<TransferAssistanceRulesResponse> Handle(GetTransferAssistanceRulesQuery query, CancellationToken ct)
    {
        log.LogInformation("Reading transfer assistance rules for member {MemberId} in tenant {TenantId}",
            query.MemberId, query.TenantId);
        var settings = options.Value;
        var member = await db.Members.AsNoTracking()
            .Where(m => m.Id == query.MemberId && m.TenantId == query.TenantId)
            .Select(m => new
            {
                Active = m.IsActive && m.Status == "active" && m.User.IsActive,
                EmailVerified = m.User.EmailVerifiedAt != null,
                m.User.MfaEnabled,
                m.User.Role
            })
            .FirstOrDefaultAsync(ct);

        var active = member?.Active ?? false;
        var emailVerified = member?.EmailVerified ?? false;
        var mfaEnabled = member?.MfaEnabled ?? false;
        var isAdmin = member?.Role == CommunityAdminRole;
        var eligibility = new TransferAssistanceMyEligibility(
            active, emailVerified, mfaEnabled, isAdmin,
            active && emailVerified && !isAdmin && (!settings.RequireMfa || mfaEnabled));

        return new TransferAssistanceRulesResponse(
            settings.RequireMfa,
            Math.Clamp(settings.MatchingWindowDays, 1, 90),
            Math.Clamp(settings.InvitationWindowDays, 1, 30),
            settings.MinimumRequestAmountMur,
            settings.MaximumRequestAmountMur,
            AmountCurrency,
            settings.MaximumRequestAmountMur > 0,
            await GetVerifierRolesAsync(query.TenantId, ct),
            eligibility);
    }

    /// <summary>Roles of the tenant (with per-tenant overrides) that hold the accreditation review permission.</summary>
    private async Task<IReadOnlyList<string>> GetVerifierRolesAsync(Guid tenantId, CancellationToken ct)
    {
        var roles = await db.Roles.AsNoTracking()
            .Where(r => r.IsActive && r.Name != PlatformAdminRole)
            .Select(r => new
            {
                r.Name,
                r.Permissions,
                TenantPermissions = db.TenantRoles
                    .Where(tr => tr.TenantId == tenantId && tr.RoleId == r.Id)
                    .Select(tr => tr.Permissions)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);
        return roles
            .Where(r => HasPermission(r.TenantPermissions ?? r.Permissions, Permissions.TransferAssistanceVerifyUsers))
            .Select(r => r.Name)
            .OrderBy(n => n == CommunityAdminRole ? 0 : 1)
            .ThenBy(n => n, StringComparer.Ordinal)
            .ToList();
    }

    private bool HasPermission(string json, string permission)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(json)?.Contains(permission, StringComparer.Ordinal) ?? false;
        }
        catch (JsonException ex)
        {
            log.LogWarning(ex, "Ignoring role with invalid permission JSON while listing accreditation reviewers");
            return false;
        }
    }
}
