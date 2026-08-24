using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Infrastructure.BackgroundServices;

public class ChargeGenerationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ChargeGenerationBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ChargeGenerationBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunGenerationAsync(stoppingToken);

            var nextMidnight = DateTime.UtcNow.Date.AddDays(1);
            var delay = nextMidnight - DateTime.UtcNow;
            logger.LogInformation("Next charge generation run at {Next}", nextMidnight);
            await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task RunGenerationAsync(CancellationToken ct)
    {
        logger.LogInformation("Running automatic charge generation at {Time}", DateTime.UtcNow);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notif = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var types = await db.ContributionTypes
            .Where(t => t.IsActive && t.EffectiveFrom <= today)
            .Where(t => t.EffectiveTo == null || t.EffectiveTo >= today)
            .Where(t => t.Frequency != "one_time")
            .ToListAsync(ct);

        if (types.Count == 0)
        {
            logger.LogInformation("No active recurring contribution types found");
            return;
        }

        var tenantIds = types.Select(t => t.TenantId).Distinct().ToList();

        var members = await db.Members
            .Include(m => m.User)
            .Where(m => tenantIds.Contains(m.TenantId) && m.IsActive)
            .ToListAsync(ct);

        int generated = 0;

        foreach (var type in types)
        {
            var dueDate = ComputeDueDate(type.Frequency, today);
            if (dueDate is null) continue;

            var tenantMembers = members.Where(m => m.TenantId == type.TenantId).ToList();

            foreach (var member in tenantMembers)
            {
                var exists = await db.ContributionCharges.AnyAsync(
                    c => c.MemberId == member.Id
                      && c.ContributionTypeId == type.Id
                      && c.DueDate == dueDate.Value,
                    ct);

                if (exists) continue;

                db.ContributionCharges.Add(new Domain.Entities.ContributionCharge
                {
                    Id = Guid.NewGuid(),
                    TenantId = type.TenantId,
                    MemberId = member.Id,
                    ContributionTypeId = type.Id,
                    DueDate = dueDate.Value,
                    BaseAmount = type.BaseAmount,
                    PenaltyAmount = 0,
                    WaiverAmount = 0,
                    AmountPaid = 0,
                    Status = "pending",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });

                generated++;

                // Notify the member (in-app + email)
                var memberName = $"{member.User.FirstName} {member.User.LastName}";
                await notif.NotifyWithEmailAsync(
                    tenantId: type.TenantId,
                    userId: member.UserId,
                    type: "charge_generated",
                    title: "Nouvelle échéance de cotisation",
                    body: $"{type.Name} — {type.BaseAmount:N0} FCFA — échéance : {dueDate.Value:dd/MM/yyyy}",
                    emailSubject: $"Nouvelle échéance : {type.Name}",
                    emailHtml: EmailTemplates.ChargeGenerated(memberName, type.Name, type.BaseAmount, dueDate.Value.ToString("dd/MM/yyyy")),
                    ct);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Charge generation complete: {Count} charge(s) created", generated);
    }

    private static DateOnly? ComputeDueDate(string frequency, DateOnly today) => frequency switch
    {
        "monthly"   => new DateOnly(today.Year, today.Month, 1),
        "quarterly" => new DateOnly(today.Year, ((today.Month - 1) / 3 * 3) + 1, 1),
        "annual"    => new DateOnly(today.Year, 1, 1),
        _ => null,
    };
}
