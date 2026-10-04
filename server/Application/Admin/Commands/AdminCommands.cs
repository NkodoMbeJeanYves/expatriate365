using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Admin.DTOs;
using server.Application.Admin.Queries;
using server.Application.Common;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Admin.Commands;

public record InviteUserCommand(Guid TenantId, InviteUserRequest Request, string CallerRole, string Lang = "fr")
    : IRequest<ServiceResult<AdminUserDto>>;

public class InviteUserCommandHandler(
    AppDbContext db,
    IEmailService emailService,
    IConfiguration config,
    ILogger<InviteUserCommandHandler> log)
    : IRequestHandler<InviteUserCommand, ServiceResult<AdminUserDto>>
{
    private static readonly string[] SuperAdminOnlyRoles = ["super_admin"];

    public async Task<ServiceResult<AdminUserDto>> Handle(InviteUserCommand request, CancellationToken ct)
    {
        var req = request.Request;
        var email = req.Email.ToLowerInvariant();

        if (SuperAdminOnlyRoles.Contains(req.Role) && request.CallerRole != "super_admin")
            return ServiceResult<AdminUserDto>.Failure(
                "Vous n'êtes pas autorisé à attribuer ce rôle.",
                "errors.auth.forbidden");

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existing is not null)
            return ServiceResult<AdminUserDto>.Failure("Un utilisateur avec cet email existe déjà.", "errors.user.email_exists");

        var tenant = await db.Tenants.FindAsync([request.TenantId], ct);

        var (plainToken, tokenHash) = TokenGenerator.Generate();

        var user = new User
        {
            Id                       = Guid.NewGuid(),
            TenantId                 = request.TenantId,
            Email                    = email,
            ContactEmail             = email,
            FirstName                = req.FirstName,
            LastName                 = req.LastName,
            Phone                    = req.Phone,
            Role                     = req.Role,
            PasswordHash             = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
            Status                   = "pending",
            IsActive                 = true,
            ActivationTokenHash      = tokenHash,
            ActivationTokenExpiresAt = DateTime.UtcNow.AddHours(72),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var baseUrl       = config["FrontendBaseUrl"] ?? config["App:BaseUrl"] ?? "https://app.expatriate365.mu";
        var setPasswordUrl = $"{baseUrl}/set-password?token={plainToken}";
        var lang          = request.Lang;
        var assocName     = tenant?.Name ?? "";

        _ = emailService.SendAsync(
            email, user.FullName,
            EmailTemplates.Subjects.MemberInvitation(assocName, lang),
            EmailTemplates.MemberInvitation(user.FullName, assocName, setPasswordUrl, lang),
            ct);

        log.LogInformation("User {UserId} ({Role}) invited to tenant {TenantId}", user.Id, user.Role, request.TenantId);

        return ServiceResult<AdminUserDto>.Success(ListAdminUsersQueryHandler.ToDto(user));
    }
}

public record ChangeUserRoleCommand(Guid TenantId, Guid UserId, ChangeRoleRequest Request, string CallerRole)
    : IRequest<ServiceResult<AdminUserDto>>;

public class ChangeUserRoleCommandHandler(AppDbContext db)
    : IRequestHandler<ChangeUserRoleCommand, ServiceResult<AdminUserDto>>
{
    private static readonly string[] SuperAdminOnlyRoles = ["super_admin"];

    public async Task<ServiceResult<AdminUserDto>> Handle(ChangeUserRoleCommand request, CancellationToken ct)
    {
        if (SuperAdminOnlyRoles.Contains(request.Request.Role) && request.CallerRole != "super_admin")
            return ServiceResult<AdminUserDto>.Failure(
                "Vous n'êtes pas autorisé à attribuer ce rôle.",
                "errors.auth.forbidden");

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Id == request.UserId && u.TenantId == request.TenantId, ct);
        if (user is null) return ServiceResult<AdminUserDto>.Failure("Utilisateur introuvable.", "errors.user.not_found");

        user.Role = request.Request.Role;
        await db.SaveChangesAsync(ct);
        return ServiceResult<AdminUserDto>.Success(ListAdminUsersQueryHandler.ToDto(user));
    }
}

public record ToggleUserStatusCommand(Guid TenantId, Guid UserId, bool Activate)
    : IRequest<ServiceResult<AdminUserDto>>;

public class ToggleUserStatusCommandHandler(AppDbContext db)
    : IRequestHandler<ToggleUserStatusCommand, ServiceResult<AdminUserDto>>
{
    public async Task<ServiceResult<AdminUserDto>> Handle(ToggleUserStatusCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Id == request.UserId && u.TenantId == request.TenantId, ct);
        if (user is null) return ServiceResult<AdminUserDto>.Failure("Utilisateur introuvable.", "errors.user.not_found");

        user.Status = request.Activate ? "active" : "suspended";
        user.IsActive = request.Activate;
        await db.SaveChangesAsync(ct);
        return ServiceResult<AdminUserDto>.Success(ListAdminUsersQueryHandler.ToDto(user));
    }
}
