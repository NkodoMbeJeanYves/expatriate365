using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Auth.DTOs;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Auth.Queries;

public record GetMeQuery(Guid UserId) : IRequest<ServiceResult<MeResponse>>;

public class GetMeQueryHandler(AppDbContext db) : IRequestHandler<GetMeQuery, ServiceResult<MeResponse>>
{
    private static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        ["super_admin"]  = ["tenants.manage", "users.manage", "members.manage", "finances.manage", "reports.read"],
        ["president"]    = ["members.manage", "finances.read", "events.manage", "meetings.manage", "welfare.approve", "elections.manage", "reports.read"],
        ["vice_president"] = ["members.read", "events.manage", "meetings.manage", "reports.read"],
        ["secretary"]    = ["members.manage", "meetings.manage", "documents.manage", "communications.send"],
        ["treasurer"]    = ["finances.manage", "payments.manage", "welfare.disburse", "reports.read"],
        ["accountant"]   = ["finances.read", "payments.read"],
        ["member"]       = ["members.read_self", "events.read", "welfare.request"],
        ["honorary_member"] = ["events.read"],
        ["guest"]        = ["events.read"],
        ["auditor"]      = ["finances.read", "reports.read"],
    };

    public async Task<ServiceResult<MeResponse>> Handle(GetMeQuery request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive, ct);
        if (user is null) return ServiceResult<MeResponse>.Failure("Utilisateur introuvable.");

        var perms = RolePermissions.TryGetValue(user.Role, out var p) ? p : [];
        return ServiceResult<MeResponse>.Success(new MeResponse(
            user.Id.ToString(),
            user.Email,
            user.FullName,
            [user.Role],
            user.TenantId?.ToString(),
            "user",
            user.Id.ToString(),
            user.EmailVerifiedAt?.ToString("O"),
            perms
        ));
    }
}
