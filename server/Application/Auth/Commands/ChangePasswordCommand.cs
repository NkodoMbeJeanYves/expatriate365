using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Auth.Commands;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record ChangePasswordCommand(Guid UserId, ChangePasswordRequest Dto)
    : IRequest<ServiceResult<bool>>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.Dto.CurrentPassword).NotEmpty();
        RuleFor(x => x.Dto.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class ChangePasswordCommandHandler(AppDbContext db, ILogger<ChangePasswordCommandHandler> log)
    : IRequestHandler<ChangePasswordCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([request.UserId], ct);
        if (user is null)
            return ServiceResult<bool>.Failure("Utilisateur introuvable.");

        if (!BCrypt.Net.BCrypt.Verify(request.Dto.CurrentPassword, user.PasswordHash))
            return ServiceResult<bool>.Failure("Mot de passe actuel incorrect.");

        if (BCrypt.Net.BCrypt.Verify(request.Dto.NewPassword, user.PasswordHash))
            return ServiceResult<bool>.Failure("Le nouveau mot de passe doit être différent de l'actuel.");

        user.PasswordHash         = BCrypt.Net.BCrypt.HashPassword(request.Dto.NewPassword);
        user.RefreshTokenHash     = null;
        user.RefreshTokenExpiresAt = null;
        user.UpdatedAt            = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("User {UserId} changed their password", user.Id);
        return ServiceResult<bool>.Success(true);
    }
}
