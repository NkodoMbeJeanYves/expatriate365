using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace server.Infrastructure.Services;

public class SmtpEmailService(IOptions<EmailSettings> opts, ILogger<SmtpEmailService> log) : IEmailService
{
    private readonly EmailSettings _cfg = opts.Value;

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_cfg.Smtp.Host))
        {
            log.LogWarning("SMTP not configured — email to {Email} skipped", toEmail);
            return;
        }

        try
        {
            using var client = new SmtpClient(_cfg.Smtp.Host, _cfg.Smtp.Port)
            {
                EnableSsl  = _cfg.Smtp.EnableSsl,
                Credentials = new NetworkCredential(_cfg.Smtp.Username, _cfg.Smtp.Password),
                Timeout    = 10_000,
            };

            var from = new MailAddress(_cfg.From, _cfg.FromName);
            var to   = new MailAddress(toEmail, toName);

            using var message = new MailMessage(from, to)
            {
                Subject    = subject,
                Body       = htmlBody,
                IsBodyHtml = true,
            };

            await client.SendMailAsync(message, ct);
            log.LogInformation("Email sent to {Email} — subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}
