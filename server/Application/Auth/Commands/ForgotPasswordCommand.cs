using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Auth.Commands;

public record ForgotPasswordCommand(string ContactEmail) : IRequest<ServiceResult<bool>>;

public class ForgotPasswordCommandHandler(
    AppDbContext db,
    IEmailService emailService,
    IConfiguration config,
    ILogger<ForgotPasswordCommandHandler> log)
    : IRequestHandler<ForgotPasswordCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var email = request.ContactEmail.ToLowerInvariant().Trim();

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.ContactEmail == email && u.IsActive, ct);

        // Always return success to avoid email enumeration
        if (user is null)
        {
            log.LogInformation("Password reset requested for unknown contact_email {Email}", email);
            return ServiceResult<bool>.Success(true);
        }

        var (plainToken, tokenHash) = server.Application.Common.TokenGenerator.Generate();

        user.PasswordResetTokenHash      = tokenHash;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt                   = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var baseUrl  = config["FrontendBaseUrl"] ?? config["App:BaseUrl"] ?? "https://app.expatriate365.mu";
        var resetUrl = $"{baseUrl}/reset-password/{plainToken}";

        _ = emailService.SendAsync(
            user.ContactEmail!,
            user.FullName,
            EmailTemplates.Subjects.PasswordReset(user.PreferredLanguage),
            EmailTemplates.PasswordReset(user.FullName, resetUrl, user.PreferredLanguage),
            ct);

        log.LogInformation("Password reset token sent to contact_email of user {UserId}", user.Id);
        return ServiceResult<bool>.Success(true);
    }
}
