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

        // Generate token: 32 random bytes, lowercase hex transmitted, SHA-256 hex stored
        var tokenBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var plainToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();
        var tokenHash  = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(tokenBytes)
        ).ToLowerInvariant();

        user.PasswordResetTokenHash      = tokenHash;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt                   = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var baseUrl  = config["App:BaseUrl"] ?? "https://app.expatriate365.mu";
        var resetUrl = $"{baseUrl}/reset-password/{plainToken}";

        _ = emailService.SendAsync(
            user.ContactEmail!,
            user.FullName,
            "Réinitialisation de votre mot de passe",
            EmailTemplates.PasswordReset(user.FullName, resetUrl),
            ct);

        log.LogInformation("Password reset token sent to contact_email of user {UserId}", user.Id);
        return ServiceResult<bool>.Success(true);
    }
}
