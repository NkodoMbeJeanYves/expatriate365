using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Auth.DTOs;
using server.Application.Common;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Auth.Commands;

public record LoginCommand(LoginRequest Dto) : IRequest<ServiceResult<LoginResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Dto.Password).NotEmpty();
    }
}

public class LoginCommandHandler(AppDbContext db, JwtService jwt, ILogger<LoginCommandHandler> log)
    : IRequestHandler<LoginCommand, ServiceResult<LoginResponse>>
{
    public async Task<ServiceResult<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        log.LogInformation("Login attempt: {Email}", dto.Email);

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email == dto.Email.ToLowerInvariant() && u.IsActive, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return ServiceResult<LoginResponse>.Failure("Email ou mot de passe incorrect.");

        if (user.Status != "active")
            return ServiceResult<LoginResponse>.Failure("Ce compte est suspendu.");

        var (plain, hash) = jwt.GenerateRefreshToken();
        user.RefreshTokenHash = hash;
        user.RefreshTokenExpiresAt = jwt.RefreshTokenExpiry();
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("Login success: {UserId}", user.Id);
        return ServiceResult<LoginResponse>.Success(new LoginResponse(
            jwt.GenerateAccessToken(user),
            plain,
            jwt.AccessExpirySeconds,
            jwt.ToUserInfo(user)
        ));
    }
}
