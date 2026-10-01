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
    IEmailService emailService,
    IConfiguration config)
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

        var memberName = $"{member.User.FirstName} {member.User.LastName}";
        var (plainToken, tokenHash) = server.Application.Common.TokenGenerator.Generate();
        member.User.ActivationTokenHash      = tokenHash;
        member.User.ActivationTokenExpiresAt = DateTime.UtcNow.AddHours(72);
        member.User.UpdatedAt                = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var baseUrl = config["FrontendBaseUrl"] ?? config["App:BaseUrl"] ?? "https://app.expatriate365.mu";
        var setPasswordUrl = $"{baseUrl}/set-password?token={plainToken}";
        log.LogInformation("Activation token generated for member {MembershipNumber}", member.MembershipNumber);

        _ = emailService.SendAsync(
            member.User.ContactEmail, memberName,
            "Activez votre compte",
            EmailTemplates.MemberInvitation(memberName, member.Tenant.Name, setPasswordUrl),
            ct);

        return ServiceResult<bool>.Success(true);
    }
}
