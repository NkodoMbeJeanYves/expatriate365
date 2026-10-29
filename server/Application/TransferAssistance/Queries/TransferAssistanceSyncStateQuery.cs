using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.TransferAssistance.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.TransferAssistance.Queries;

public static class TransferAssistanceSyncSections
{
    public const string Mine = "mine";
    public const string Available = "available";
    public const string Assignments = "assignments";
    public const string WitnessInvitations = "witness_invitations";
    public const string BeneficiaryOpen = "beneficiary_open";
    public const string ReviewQueue = "review_queue";
}

public record GetTransferAssistanceSyncStateQuery(
    Guid TenantId,
    Guid? MemberId,
    IReadOnlyCollection<string> Permissions)
    : IRequest<TransferAssistanceSyncStateResponse>;

public class GetTransferAssistanceSyncStateHandler(
    AppDbContext db,
    ILogger<GetTransferAssistanceSyncStateHandler> log)
    : IRequestHandler<GetTransferAssistanceSyncStateQuery, TransferAssistanceSyncStateResponse>
{
    public async Task<TransferAssistanceSyncStateResponse> Handle(
        GetTransferAssistanceSyncStateQuery query,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var tenantId = query.TenantId;
        var sections = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var requests = db.TransferAssistanceRequests.AsNoTracking().Where(r => r.TenantId == tenantId);

        if (query.MemberId is { } memberId)
        {
            if (query.Permissions.Contains(Permissions.TransferAssistanceReadOwn))
            {
                var mine = requests.Where(r => r.RequesterMemberId == memberId);
                sections[TransferAssistanceSyncSections.Mine] = Token(
                    await StampAsync(mine, r => r.UpdatedAt ?? r.CreatedAt, ct),
                    await StampAsync(db.TransferAssistanceOffers.AsNoTracking()
                        .Where(o => o.TenantId == tenantId && o.Request.RequesterMemberId == memberId &&
                                    o.Status == TransferAssistanceOfferStatuses.Pending), o => o.CreatedAt, ct),
                    await StampAsync(db.TransferAssistanceWitnessInvitations.AsNoTracking()
                        .Where(i => i.TenantId == tenantId && i.Request.RequesterMemberId == memberId),
                        i => i.RespondedAt ?? i.CreatedAt, ct),
                    await StampAsync(db.TransferAssistanceBeneficiaryCandidates.AsNoTracking()
                        .Where(c => c.TenantId == tenantId && c.Request.RequesterMemberId == memberId),
                        c => c.UpdatedAt ?? c.CreatedAt, ct));

                sections[TransferAssistanceSyncSections.BeneficiaryOpen] = Token(
                    await StampAsync(requests.Where(r => r.IsActive && r.BeneficiaryMemberId == null &&
                            r.ExpiresAt > now && r.RequesterMemberId != memberId &&
                            (r.Status == TransferAssistanceStatuses.Matching ||
                            r.Status == TransferAssistanceStatuses.WitnessPending ||
                            r.Status == TransferAssistanceStatuses.IntermediaryAssigned ||
                            r.Status == TransferAssistanceStatuses.MeetingScheduled ||
                            r.Status == TransferAssistanceStatuses.TransferInProgress)),
                        r => r.UpdatedAt ?? r.CreatedAt, ct),
                    await StampAsync(db.TransferAssistanceBeneficiaryCandidates.AsNoTracking()
                        .Where(c => c.TenantId == tenantId && c.MemberId == memberId),
                        c => c.UpdatedAt ?? c.CreatedAt, ct));
            }

            if (query.Permissions.Contains(Permissions.TransferAssistanceViewAvailable))
                sections[TransferAssistanceSyncSections.Available] = Token(
                    await StampAsync(requests.Where(r => r.IsActive &&
                            r.Status == TransferAssistanceStatuses.Matching && r.ExpiresAt > now &&
                            r.RequesterMemberId != memberId),
                        r => r.UpdatedAt ?? r.CreatedAt, ct),
                    await StampAsync(db.TransferAssistanceOffers.AsNoTracking()
                        .Where(o => o.TenantId == tenantId && o.HelperMemberId == memberId &&
                                    o.Status == TransferAssistanceOfferStatuses.Pending), o => o.CreatedAt, ct));

            if (query.Permissions.Contains(Permissions.TransferAssistanceParticipate))
            {
                sections[TransferAssistanceSyncSections.Assignments] = Token(
                    await StampAsync(requests.Where(r => r.IsActive &&
                            (r.HelperMemberId == memberId || r.WitnessMemberId == memberId ||
                             r.BeneficiaryMemberId == memberId)),
                        r => r.UpdatedAt ?? r.CreatedAt, ct));
                sections[TransferAssistanceSyncSections.WitnessInvitations] = Token(
                    await StampAsync(db.TransferAssistanceWitnessInvitations.AsNoTracking()
                        .Where(i => i.TenantId == tenantId && i.WitnessMemberId == memberId &&
                                    i.Status == TransferAssistanceWitnessInvitationStatuses.Pending &&
                                    i.ExpiresAt > now),
                        i => i.RespondedAt ?? i.CreatedAt, ct));
            }
        }

        if (query.Permissions.Contains(Permissions.TransferAssistanceReview))
            sections[TransferAssistanceSyncSections.ReviewQueue] = Token(
                await StampAsync(requests.Where(r => r.IsActive &&
                        (r.Status == TransferAssistanceStatuses.Submitted ||
                         r.Status == TransferAssistanceStatuses.Disputed ||
                         r.Status == TransferAssistanceStatuses.Suspended)),
                    r => r.UpdatedAt ?? r.CreatedAt, ct));

        var version = Hash(string.Join('|', sections.Select(s => $"{s.Key}={s.Value}")));
        log.LogDebug("Computed transfer assistance sync state {Version} for member {MemberId}", version, query.MemberId);
        return new TransferAssistanceSyncStateResponse(version, sections, now);
    }

    private static async Task<string> StampAsync<T>(
        IQueryable<T> source,
        Expression<Func<T, DateTime>> changedAt,
        CancellationToken ct)
    {
        var count = await source.CountAsync(ct);
        if (count == 0) return "0";
        var latest = await source.MaxAsync(changedAt, ct);
        return $"{count}:{latest.Ticks}";
    }

    private static string Token(params string[] stamps) => Hash(string.Join(';', stamps));

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16];
}
