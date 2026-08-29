using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;

namespace server.Infrastructure.BackgroundServices;

/// <summary>
/// Runs once per day at midnight and auto-generates ContributionCharges for every active
/// ContributionType × active Member combination that does not yet have a charge for the
/// current billing period.
/// </summary>
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

        // Collect all tenants present in active types
        var tenantIds = types.Select(t => t.TenantId).Distinct().ToList();

        var members = await db.Members
            .Where(m => tenantIds.Contains(m.TenantId) && m.IsActive)
            .ToListAsync(ct);

        // Pre-load all existing charges for the relevant types/due-dates in one query
        // to avoid N+1 (one AnyAsync per member×type combination).
        var typeIds  = types.Select(t => t.Id).ToList();
        var dueDates = types
            .Select(t => ComputeDueDate(t.Frequency, today))
            .Where(d => d.HasValue).Select(d => d!.Value)
            .Distinct().ToList();

        var existingKeys = await db.ContributionCharges
            .Where(c => typeIds.Contains(c.ContributionTypeId) && dueDates.Contains(c.DueDate))
            .Select(c => new { c.MemberId, c.ContributionTypeId, c.DueDate })
            .ToListAsync(ct);

        var existingSet = existingKeys
            .Select(c => (c.MemberId, c.ContributionTypeId, c.DueDate))
            .ToHashSet();

        int generated = 0;

        foreach (var type in types)
        {
            var dueDate = ComputeDueDate(type.Frequency, today);
            if (dueDate is null) continue;

            var tenantMembers = members.Where(m => m.TenantId == type.TenantId).ToList();

            foreach (var member in tenantMembers)
            {
                if (existingSet.Contains((member.Id, type.Id, dueDate.Value))) continue;

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
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Charge generation complete: {Count} charge(s) created", generated);
    }

    /// <summary>
    /// Returns the DueDate for the current billing period of the given frequency.
    /// Returns null for unsupported/one_time frequencies.
    /// </summary>
    private static DateOnly? ComputeDueDate(string frequency, DateOnly today) => frequency switch
    {
        // First day of the current month
        "monthly" => new DateOnly(today.Year, today.Month, 1),

        // First day of the current quarter (Q1=Jan, Q2=Apr, Q3=Jul, Q4=Oct)
        "quarterly" => new DateOnly(today.Year, ((today.Month - 1) / 3 * 3) + 1, 1),

        // First day of the current year
        "annual" => new DateOnly(today.Year, 1, 1),

        _ => null,
    };
}
