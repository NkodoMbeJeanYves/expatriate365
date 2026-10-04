using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Auth.Commands;

public record SetPasswordRequest(string Token, string NewPassword);

public record SetPasswordCommand(SetPasswordRequest Dto) : IRequest<ServiceResult<bool>>;

public class SetPasswordCommandValidator : AbstractValidator<SetPasswordCommand>
{
    public SetPasswordCommandValidator()
    {
        RuleFor(x => x.Dto.Token).NotEmpty();
        RuleFor(x => x.Dto.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class SetPasswordCommandHandler(AppDbContext db, ILogger<SetPasswordCommandHandler> log)
    : IRequestHandler<SetPasswordCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(SetPasswordCommand request, CancellationToken ct)
    {
        var tokenHash = TokenGenerator.Hash(request.Dto.Token);
        if (tokenHash is null)
            return ServiceResult<bool>.Failure("Lien d'activation invalide.", "errors.auth.invalid_activation_link");

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.ActivationTokenHash == tokenHash && u.IsActive, ct);

        if (user is null)
            return ServiceResult<bool>.Failure("Lien d'activation invalide ou déjà utilisé.", "errors.auth.invalid_activation_link");

        if (user.ActivationTokenExpiresAt is null || user.ActivationTokenExpiresAt < DateTime.UtcNow)
            return ServiceResult<bool>.Failure("Ce lien a expiré. Demandez un nouvel email d'activation.", "errors.auth.activation_link_expired");

        user.PasswordHash           = BCrypt.Net.BCrypt.HashPassword(request.Dto.NewPassword);
        user.EmailVerifiedAt        = DateTime.UtcNow;
        user.ActivationTokenHash    = null;
        user.ActivationTokenExpiresAt = null;
        user.RefreshTokenHash       = null;
        user.RefreshTokenExpiresAt  = null;
        user.UpdatedAt              = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("Account activated for user {UserId}", user.Id);
        return ServiceResult<bool>.Success(true);
    }
}
