using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Auth.Commands;

public record LogoutCommand(string RefreshToken) : IRequest<ServiceResult<bool>>;

public class LogoutCommandHandler(AppDbContext db, ILogger<LogoutCommandHandler> log)
    : IRequestHandler<LogoutCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(LogoutCommand request, CancellationToken ct)
    {
        var hash = JwtService.HashToken(request.RefreshToken);
        var user = await db.Users.FirstOrDefaultAsync(u => u.RefreshTokenHash == hash, ct);

        if (user is not null)
        {
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            await db.SaveChangesAsync(ct);
            log.LogInformation("Logout: {UserId}", user.Id);
        }

        return ServiceResult<bool>.Success(true);
    }
}
