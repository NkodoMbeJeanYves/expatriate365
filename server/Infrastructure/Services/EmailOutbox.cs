using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Infrastructure.Services;

public sealed class EmailOutboxOptions
{
    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 20;
    public int PollSeconds { get; set; } = 15;
    public int MaxAttempts { get; set; } = 5;
    public int RetentionDays { get; set; } = 30;
}

public static class EmailOutboxTemplates
{
    public const string TransferAssistanceEvent = "transfer_assistance.event";
    public const string PreRenderedHtml = "pre_rendered_html";
}

/// <summary>
/// Adds outgoing emails to the persistent outbox. Rows are only persisted with the caller's
/// SaveChanges, so no email leaves for a business transition that was rolled back.
/// </summary>
public static class EmailOutbox
{
    public static async Task<bool> EnqueueAsync(
        AppDbContext db,
        Guid? tenantId,
        Guid userId,
        string toEmail,
        string toName,
        string language,
        string template,
        object payload,
        string dedupKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return false;
        if (db.EmailOutbox.Local.Any(m => m.DedupKey == dedupKey) ||
            await db.EmailOutbox.AsNoTracking().AnyAsync(m => m.DedupKey == dedupKey, ct))
            return false;

        db.EmailOutbox.Add(new EmailOutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            ToEmail = toEmail.Trim(),
            ToName = toName,
            Language = language == "en" ? "en" : "fr",
            Template = template,
            PayloadJson = JsonSerializer.Serialize(payload),
            DedupKey = dedupKey.Length > 200 ? dedupKey[..200] : dedupKey,
            NextAttemptAt = DateTime.UtcNow,
        });
        return true;
    }

    public static string ThrottleBucket(DateTime utcNow, int throttleMinutes)
    {
        var window = TimeSpan.FromMinutes(Math.Clamp(throttleMinutes, 1, 24 * 60));
        return (utcNow.Ticks / window.Ticks).ToString();
    }
}
