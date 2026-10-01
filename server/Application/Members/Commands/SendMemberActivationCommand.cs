using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Members.Commands;

public record SendMemberActivationCommand(Guid TenantId, Guid MemberId)
    : IRequest<ServiceResult<bool>>;

public class SendMemberActivationHandler(
    AppDbContext db,
    ILogger<SendMemberActivationHandler> log,
    IEmailService emailService)
    : IRequestHandler<SendMemberActivationCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(SendMemberActivationCommand request, CancellationToken ct)
    {
        var member = await db.Members
            .Include(m => m.User)
            .Include(m => m.Tenant)
            .FirstOrDefaultAsync(m => m.Id == request.MemberId && m.TenantId == request.TenantId, ct);

        if (member is null)
            return ServiceResult<bool>.Failure("Membre introuvable.");
        if (member.User is null)
            return ServiceResult<bool>.Failure("Aucun compte utilisateur associé à ce membre.");
        if (member.User.EmailVerifiedAt.HasValue)
            return ServiceResult<bool>.Failure("Ce compte est déjà activé.");
        if (string.IsNullOrWhiteSpace(member.User.ContactEmail))
            return ServiceResult<bool>.Failure("Ce membre n'a pas d'adresse email de contact renseignée.");

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var setPasswordUrl = $"https://app.expatriate365.mu/set-password?token={token}";
        var memberName = $"{member.User.FirstName} {member.User.LastName}";

        log.LogInformation("[ACTIVATION] Member {MembershipNumber} | ContactEmail: {Email} | Token: {Token}",
            member.MembershipNumber, member.User.ContactEmail, token);

        _ = emailService.SendAsync(
            member.User.ContactEmail, memberName,
            "Activez votre compte",
            EmailTemplates.MemberInvitation(memberName, member.Tenant.Name, setPasswordUrl),
            ct);

        return ServiceResult<bool>.Success(true);
    }
}
