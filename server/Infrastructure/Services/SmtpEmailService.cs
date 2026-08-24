using System.Net;
using System.Net.Mail;

namespace server.Infrastructure.Services;

public class SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> log) : IEmailService
{
    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        var smtp = config.GetSection("Smtp");
        var host = smtp["Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            log.LogWarning("SMTP not configured — email to {Email} skipped", toEmail);
            return;
        }

        try
        {
            using var client = new SmtpClient(host, int.Parse(smtp["Port"] ?? "587"))
            {
                EnableSsl = bool.Parse(smtp["EnableSsl"] ?? "true"),
                Credentials = new NetworkCredential(smtp["Username"], smtp["Password"]),
                Timeout = 10_000,
            };

            var from = new MailAddress(smtp["FromAddress"] ?? smtp["Username"]!, smtp["FromName"] ?? "Expatriate365");
            var to = new MailAddress(toEmail, toName);

            using var message = new MailMessage(from, to)
            {
                Subject = subject,
                Body = htmlBody,
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
