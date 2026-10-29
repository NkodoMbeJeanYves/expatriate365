namespace server.Infrastructure.Services;

public enum EmailSendResult
{
    Sent,
    TransientFailure,
    NotConfigured,
}

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default);

    async Task<EmailSendResult> TrySendAsync(
        string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        await SendAsync(toEmail, toName, subject, htmlBody, ct);
        return EmailSendResult.Sent;
    }
}
