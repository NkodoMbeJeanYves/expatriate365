using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Auth.Commands;

public record ResetPasswordRequest(string Token, string NewPassword);

public record ResetPasswordCommand(ResetPasswordRequest Dto) : IRequest<ServiceResult<bool>>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Dto.Token).NotEmpty();
        RuleFor(x => x.Dto.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class ResetPasswordCommandHandler(AppDbContext db, ILogger<ResetPasswordCommandHandler> log)
    : IRequestHandler<ResetPasswordCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var tokenHash = server.Application.Common.TokenGenerator.Hash(request.Dto.Token);
        if (tokenHash is null)
            return ServiceResult<bool>.Failure("Lien de réinitialisation invalide.", "errors.auth.invalid_reset_link");

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.PasswordResetTokenHash == tokenHash && u.IsActive, ct);

        if (user is null)
            return ServiceResult<bool>.Failure("Lien de réinitialisation invalide ou déjà utilisé.", "errors.auth.invalid_reset_link");

        if (user.PasswordResetTokenExpiresAt is null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            return ServiceResult<bool>.Failure("Ce lien a expiré. Veuillez faire une nouvelle demande.", "errors.auth.reset_link_expired");

        user.PasswordHash                = BCrypt.Net.BCrypt.HashPassword(request.Dto.NewPassword);
        user.PasswordResetTokenHash      = null;
        user.PasswordResetTokenExpiresAt = null;
        user.RefreshTokenHash            = null;
        user.RefreshTokenExpiresAt       = null;
        user.UpdatedAt                   = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("Password reset completed for user {UserId}", user.Id);
        return ServiceResult<bool>.Success(true);
    }
}
