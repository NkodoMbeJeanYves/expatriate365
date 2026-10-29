using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using server.Application.TransferAssistance;
using server.Application.TransferAssistance.Commands;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Infrastructure.BackgroundServices;

public sealed class TransferAssistanceExpirationService(
    IServiceScopeFactory scopeFactory,
    ILogger<TransferAssistanceExpirationService> log)
    : BackgroundService
{
    public const string ExpiryWarningEvent = "expiry_warning_sent";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await ExpireWorkflowItemsAsync(
                    scope.ServiceProvider.GetRequiredService<AppDbContext>(),
                    scope.ServiceProvider.GetRequiredService<IOptions<TransferAssistanceOptions>>().Value,
                    log,
                    stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                log.LogError(exception, "Transfer assistance expiration cycle failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public static async Task ExpireWorkflowItemsAsync(
        AppDbContext db,
        TransferAssistanceOptions options,
        ILogger log,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var requests = await db.TransferAssistanceRequests.Where(r =>
            r.IsActive && r.ExpiresAt <= now &&
            (r.Status == TransferAssistanceStatuses.Matching ||
             r.Status == TransferAssistanceStatuses.WitnessPending ||
             r.Status == TransferAssistanceStatuses.MeetingScheduled)).ToListAsync(ct);
        foreach (var request in requests)
        {
            request.Status = TransferAssistanceStatuses.Expired;
            request.OpenRequestKey = null;
            request.WorkflowVersion++;
            db.TransferAssistanceEvents.Add(new TransferAssistanceEvent
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                RequestId = request.Id,
                EventType = "request_expired",
                Details = "The configured assistance workflow deadline elapsed.",
                CreatedAt = now,
            });
            await TransferAssistanceWorkflowSupport.ClosePendingCandidatesAsync(db, request.Id, ct);
            await TransferAssistanceWorkflowSupport.NotifyParticipantsAsync(db, request, null,
                TransferAssistanceNotificationTypes.Expiration, "Request expired",
                "The assistance request expired before the process was completed.",
                options, "expired", ct);
        }

        var invitations = await db.TransferAssistanceWitnessInvitations.Where(i =>
            i.Status == TransferAssistanceWitnessInvitationStatuses.Pending && i.ExpiresAt <= now)
            .ToListAsync(ct);
        foreach (var invitation in invitations)
        {
            invitation.Status = TransferAssistanceWitnessInvitationStatuses.Expired;
            invitation.RespondedAt = now;
            if (!requests.Any(r => r.Id == invitation.RequestId))
                db.TransferAssistanceEvents.Add(new TransferAssistanceEvent
                {
                    Id = Guid.NewGuid(),
                    TenantId = invitation.TenantId,
                    RequestId = invitation.RequestId,
                    EventType = "witness_invitation_expired",
                    CreatedAt = now,
                });
        }

        var warningLimit = now.AddHours(24);
        var expiringSoon = await db.TransferAssistanceRequests
            .Include(r => r.RequesterMember)
            .Where(r => r.IsActive && r.ExpiresAt > now && r.ExpiresAt <= warningLimit &&
                        (r.Status == TransferAssistanceStatuses.Matching ||
                         r.Status == TransferAssistanceStatuses.WitnessPending ||
                         r.Status == TransferAssistanceStatuses.MeetingScheduled) &&
                        !db.TransferAssistanceEvents.Any(e => e.RequestId == r.Id &&
                            e.EventType == ExpiryWarningEvent))
            .ToListAsync(ct);
        foreach (var request in expiringSoon)
        {
            db.TransferAssistanceEvents.Add(new TransferAssistanceEvent
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                RequestId = request.Id,
                EventType = ExpiryWarningEvent,
                CreatedAt = now,
            });
            await TransferAssistanceWorkflowSupport.AddNotificationAsync(db, request.TenantId,
                request.RequesterMember.UserId, TransferAssistanceNotificationTypes.Expiration,
                "Request expiring soon", "Your assistance request will expire within 24 hours.",
                request.Id.ToString(), ct, options, "expiring_soon");
        }

        if (requests.Count != 0 || invitations.Count != 0 || expiringSoon.Count != 0)
        {
            await db.SaveChangesAsync(ct);
            log.LogInformation(
                "Expired {RequestCount} transfer assistance requests and {InvitationCount} witness invitations; warned {WarningCount} requesters",
                requests.Count, invitations.Count, expiringSoon.Count);
        }
    }
}