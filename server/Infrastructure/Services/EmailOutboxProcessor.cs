using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Infrastructure.Services;

public sealed class EmailOutboxProcessor(
    AppDbContext db,
    IEmailService email,
    IOptions<EmailOutboxOptions> options,
    IConfiguration config,
    ILogger<EmailOutboxProcessor> log)
{
    private static readonly TimeSpan[] Backoff =
    [
        TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(60),
    ];

    public async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        var settings = options.Value;
        var now = DateTime.UtcNow;
        var batch = await db.EmailOutbox
            .Where(m => m.Status == EmailOutboxStatuses.Pending && m.NextAttemptAt <= now)
            .OrderBy(m => m.CreatedAt)
            .Take(Math.Clamp(settings.BatchSize, 1, 200))
            .ToListAsync(ct);

        foreach (var message in batch)
        {
            await ProcessAsync(message, settings, ct);
            await db.SaveChangesAsync(ct);
        }

        await PurgeAsync(settings, ct);
        return batch.Count;
    }

    private async Task ProcessAsync(EmailOutboxMessage message, EmailOutboxOptions settings, CancellationToken ct)
    {
        var duplicate = await db.EmailOutbox.AsNoTracking().AnyAsync(m =>
            m.Id != message.Id && m.DedupKey == message.DedupKey &&
            (m.Status == EmailOutboxStatuses.Sent ||
             (m.Status == EmailOutboxStatuses.Pending && m.CreatedAt < message.CreatedAt)), ct);
        if (duplicate)
        {
            Skip(message, "duplicate");
            return;
        }

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == message.UserId && u.IsActive)
            .Select(u => new { u.ContactEmail, u.FirstName, u.LastName, u.PreferredLanguage })
            .FirstOrDefaultAsync(ct);
        var recipient = user?.ContactEmail?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            Skip(message, "no_contact_email");
            return;
        }

        if (!string.Equals(recipient, message.ToEmail, StringComparison.OrdinalIgnoreCase))
            message.ToEmail = recipient;
        var language = user!.PreferredLanguage == "en" ? "en" : "fr";

        (string Subject, string Html)? rendered;
        try
        {
            rendered = await RenderAsync(message, language, ct);
        }
        catch (JsonException)
        {
            rendered = null;
        }
        if (rendered is null)
        {
            message.Status = EmailOutboxStatuses.Failed;
            message.LastError = "render_failed";
            log.LogWarning("Email outbox message {MessageId} could not be rendered (template {Template})",
                message.Id, message.Template);
            return;
        }

        message.Attempts++;
        var result = await email.TrySendAsync(
            recipient, $"{user.FirstName} {user.LastName}".Trim(), rendered.Value.Subject, rendered.Value.Html, ct);
        switch (result)
        {
            case EmailSendResult.Sent:
                message.Status = EmailOutboxStatuses.Sent;
                message.SentAt = DateTime.UtcNow;
                message.LastError = null;
                log.LogInformation("Email outbox message {MessageId} sent to {Email}",
                    message.Id, EmailAddressMasking.Mask(recipient));
                break;
            case EmailSendResult.NotConfigured:
                Skip(message, "smtp_not_configured");
                break;
            default:
                if (message.Attempts >= Math.Max(1, settings.MaxAttempts))
                {
                    message.Status = EmailOutboxStatuses.Failed;
                    message.LastError = "max_attempts_reached";
                    log.LogWarning("Email outbox message {MessageId} failed after {Attempts} attempts",
                        message.Id, message.Attempts);
                }
                else
                {
                    message.LastError = "transient_failure";
                    message.NextAttemptAt = DateTime.UtcNow.Add(Backoff[Math.Min(message.Attempts - 1, Backoff.Length - 1)]);
                    log.LogInformation("Email outbox message {MessageId} will be retried at {NextAttemptAt}",
                        message.Id, message.NextAttemptAt);
                }
                break;
        }
    }

    private async Task<(string Subject, string Html)?> RenderAsync(
        EmailOutboxMessage message, string language, CancellationToken ct)
    {
        using var payload = JsonDocument.Parse(message.PayloadJson);
        var root = payload.RootElement;
        switch (message.Template)
        {
            case EmailOutboxTemplates.TransferAssistanceEvent:
            {
                var eventKey = root.TryGetProperty("event", out var value) ? value.GetString() ?? "" : "";
                var association = message.TenantId is null
                    ? "Expatriate365"
                    : await db.Tenants.AsNoTracking().Where(t => t.Id == message.TenantId)
                        .Select(t => t.Name).FirstOrDefaultAsync(ct) ?? "Expatriate365";
                var baseUrl = (config["FrontendBaseUrl"] ?? config["App:BaseUrl"] ?? "https://app.expatriate365.mu")
                    .TrimEnd('/');
                return EmailTemplates.TransferAssistanceEvent(
                    eventKey, association, $"{baseUrl}/transfer-assistance", language);
            }
            case EmailOutboxTemplates.PreRenderedHtml:
            {
                var subject = root.TryGetProperty("subject", out var s) ? s.GetString() : null;
                var html = root.TryGetProperty("html", out var h) ? h.GetString() : null;
                return string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(html)
                    ? null
                    : (subject, html);
            }
            default:
                return null;
        }
    }

    private void Skip(EmailOutboxMessage message, string reason)
    {
        message.Status = EmailOutboxStatuses.Skipped;
        message.LastError = reason;
        log.LogInformation("Email outbox message {MessageId} skipped ({Reason})", message.Id, reason);
    }

    private async Task PurgeAsync(EmailOutboxOptions settings, CancellationToken ct)
    {
        var threshold = DateTime.UtcNow.AddDays(-Math.Max(1, settings.RetentionDays));
        var expired = await db.EmailOutbox
            .Where(m => (m.Status == EmailOutboxStatuses.Sent || m.Status == EmailOutboxStatuses.Skipped) &&
                        m.CreatedAt < threshold)
            .Take(500)
            .ToListAsync(ct);
        if (expired.Count == 0) return;
        db.EmailOutbox.RemoveRange(expired);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Purged {Count} processed email outbox messages", expired.Count);
    }
}
