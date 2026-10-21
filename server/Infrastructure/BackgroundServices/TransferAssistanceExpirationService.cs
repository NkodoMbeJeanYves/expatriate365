using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Infrastructure.BackgroundServices;

public sealed class TransferAssistanceExpirationService(
    IServiceScopeFactory scopeFactory,
    ILogger<TransferAssistanceExpirationService> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            await ExpireWorkflowItemsAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExpireWorkflowItemsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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

        if (requests.Count != 0 || invitations.Count != 0)
        {
            await db.SaveChangesAsync(ct);
            log.LogInformation("Expired {RequestCount} transfer assistance requests and {InvitationCount} witness invitations",
                requests.Count, invitations.Count);
        }
    }
}
