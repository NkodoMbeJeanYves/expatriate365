using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Admin.DTOs;
using server.Application.Admin.Queries;
using server.Application.Common;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Admin.Commands;

public record InviteUserCommand(Guid TenantId, InviteUserRequest Request)
    : IRequest<ServiceResult<AdminUserDto>>;

public class InviteUserCommandHandler(AppDbContext db)
    : IRequestHandler<InviteUserCommand, ServiceResult<AdminUserDto>>
{
    public async Task<ServiceResult<AdminUserDto>> Handle(InviteUserCommand request, CancellationToken ct)
    {
        var req = request.Request;
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant(), ct);
        if (existing is not null)
            return ServiceResult<AdminUserDto>.Failure("Un utilisateur avec cet email existe déjà.");

        // Temporary password — user must reset on first login in a real implementation
        var tempPassword = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")[..12]);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Email = req.Email.ToLowerInvariant(),
            FirstName = req.FirstName,
            LastName = req.LastName,
            Phone = req.Phone,
            Role = req.Role,
            PasswordHash = tempPassword,
            Status = "pending",
            IsActive = true,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return ServiceResult<AdminUserDto>.Success(ListAdminUsersQueryHandler.ToDto(user));
    }
}

public record ChangeUserRoleCommand(Guid TenantId, Guid UserId, ChangeRoleRequest Request)
    : IRequest<ServiceResult<AdminUserDto>>;

public class ChangeUserRoleCommandHandler(AppDbContext db)
    : IRequestHandler<ChangeUserRoleCommand, ServiceResult<AdminUserDto>>
{
    public async Task<ServiceResult<AdminUserDto>> Handle(ChangeUserRoleCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Id == request.UserId && u.TenantId == request.TenantId, ct);
        if (user is null) return ServiceResult<AdminUserDto>.Failure("Utilisateur introuvable.");

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
        if (user is null) return ServiceResult<AdminUserDto>.Failure("Utilisateur introuvable.");

        user.Status = request.Activate ? "active" : "suspended";
        user.IsActive = request.Activate;
        await db.SaveChangesAsync(ct);
        return ServiceResult<AdminUserDto>.Success(ListAdminUsersQueryHandler.ToDto(user));
    }
}
