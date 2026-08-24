using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Members.Commands;

public record SendMemberActivationCommand(Guid TenantId, Guid MemberId)
    : IRequest<ServiceResult<bool>>;

public class SendMemberActivationHandler(AppDbContext db)
    : IRequestHandler<SendMemberActivationCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(SendMemberActivationCommand request, CancellationToken ct)
    {
        var member = await db.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == request.MemberId && m.TenantId == request.TenantId, ct);

        if (member is null)
            return ServiceResult<bool>.Failure("Membre introuvable.");
        if (member.User is null)
            return ServiceResult<bool>.Failure("Aucun compte utilisateur associé à ce membre.");
        if (member.User.EmailVerifiedAt.HasValue)
            return ServiceResult<bool>.Failure("Ce compte est déjà activé.");

        // Generate a simulated activation token
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        // In production: send email. For MVP: log the activation link.
        Log.Information("[ACTIVATION] Member {MembershipNumber} | Email: {Email} | Token: {Token} | Link: /activate?token={Token}",
            member.MembershipNumber, member.User.Email, token, token);

        return ServiceResult<bool>.Success(true);
    }
}
