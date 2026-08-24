using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Auth.DTOs;
using server.Application.Common;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<ServiceResult<LoginResponse>>;

public class RefreshTokenCommandHandler(AppDbContext db, JwtService jwt, ILogger<RefreshTokenCommandHandler> log)
    : IRequestHandler<RefreshTokenCommand, ServiceResult<LoginResponse>>
{
    public async Task<ServiceResult<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var hash = JwtService.HashToken(request.RefreshToken);
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.RefreshTokenHash == hash && u.IsActive, ct);

        if (user is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            log.LogWarning("Refresh token invalid or expired");
            return ServiceResult<LoginResponse>.Failure("Refresh token invalide ou expiré.");
        }

        var (plain, newHash) = jwt.GenerateRefreshToken();
        user.RefreshTokenHash = newHash;
        user.RefreshTokenExpiresAt = jwt.RefreshTokenExpiry();
        await db.SaveChangesAsync(ct);

        log.LogInformation("Token refreshed for user {UserId}", user.Id);
        return ServiceResult<LoginResponse>.Success(new LoginResponse(
            jwt.GenerateAccessToken(user),
            plain,
            jwt.AccessExpirySeconds,
            jwt.ToUserInfo(user)
        ));
    }
}
