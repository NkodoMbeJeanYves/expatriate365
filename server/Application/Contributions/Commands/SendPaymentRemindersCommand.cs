using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Contributions.Commands;

public record SendPaymentRemindersCommand(Guid TenantId) : IRequest<ServiceResult<int>>;

public class SendPaymentRemindersCommandHandler(
    AppDbContext db,
    ILogger<SendPaymentRemindersCommandHandler> log,
    INotificationService notif,
    IEmailService email)
    : IRequestHandler<SendPaymentRemindersCommand, ServiceResult<int>>
{
    public async Task<ServiceResult<int>> Handle(SendPaymentRemindersCommand request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Overdue charges: due date passed, not paid, not waived
        var overdueCharges = await db.ContributionCharges
            .Include(c => c.Member).ThenInclude(m => m.User)
            .Include(c => c.ContributionType)
            .Where(c => c.TenantId == request.TenantId
                     && c.IsActive
                     && c.Status == "pending"
                     && c.DueDate < today
                     && c.Balance > 0)
            .ToListAsync(ct);

        if (!overdueCharges.Any())
            return ServiceResult<int>.Success(0);

        var tasks = overdueCharges.Select(async c =>
        {
            var lang = c.Member.User.PreferredLanguage ?? "fr";
            var name = $"{c.Member.User.FirstName} {c.Member.User.LastName}";

            // In-app notification
            await notif.NotifyAsync(request.TenantId, c.Member.UserId,
                "charge_generated",
                lang == "fr" ? "Rappel de cotisation" : "Payment reminder",
                lang == "fr"
                    ? $"Votre cotisation {c.ContributionType.Name} est en retard. Solde restant : {c.Balance}. Veuillez régulariser dès que possible."
                    : $"Your contribution {c.ContributionType.Name} is overdue. Remaining balance: {c.Balance}. Please settle as soon as possible.",
                ct, "charge", c.Id.ToString());

            // Email reminder
            if (!string.IsNullOrWhiteSpace(c.Member.User.ContactEmail))
                await email.SendAsync(
                    c.Member.User.ContactEmail, name,
                    lang == "fr" ? $"Rappel — Cotisation en retard : {c.ContributionType.Name}" : $"Reminder — Overdue contribution: {c.ContributionType.Name}",
                    BuildReminderHtml(name, c.ContributionType.Name, c.Balance, c.DueDate, lang),
                    ct);
        });

        await Task.WhenAll(tasks);
        log.LogInformation("Sent {Count} payment reminders for tenant {TenantId}", overdueCharges.Count, request.TenantId);
        return ServiceResult<int>.Success(overdueCharges.Count);
    }

    private static string BuildReminderHtml(string name, string typeName, decimal balance, DateOnly dueDate, string lang)
    {
        return lang == "fr"
            ? $"""
<p>Bonjour {name},</p>
<p>Nous vous rappelons que votre cotisation <strong>{typeName}</strong> était due le <strong>{dueDate:dd/MM/yyyy}</strong>.</p>
<p>Solde restant : <strong>{balance}</strong></p>
<p>Merci de régulariser votre situation dès que possible.</p>
<p>Cordialement,<br/>L'équipe de votre association</p>
"""
            : $"""
<p>Hello {name},</p>
<p>This is a reminder that your contribution <strong>{typeName}</strong> was due on <strong>{dueDate:dd/MM/yyyy}</strong>.</p>
<p>Remaining balance: <strong>{balance}</strong></p>
<p>Please settle your account as soon as possible.</p>
<p>Best regards,<br/>Your association team</p>
""";
    }
}
