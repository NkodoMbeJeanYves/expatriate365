using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Members.DTOs;
using server.Application.Members.Services;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Members.Commands;

public record CreateMemberCommand(Guid TenantId, CreateMemberRequest Dto)
    : IRequest<ServiceResult<MemberDto>>;

public class CreateMemberCommandValidator : AbstractValidator<CreateMemberCommand>
{
    public CreateMemberCommandValidator()
    {
        RuleFor(x => x.Dto.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.JoinedDate).NotEmpty();
    }
}

public class CreateMemberCommandHandler(
    AppDbContext db,
    ILogger<CreateMemberCommandHandler> log,
    IEmailService emailService,
    IConfiguration config)
    : IRequestHandler<CreateMemberCommand, ServiceResult<MemberDto>>
{
    public async Task<ServiceResult<MemberDto>> Handle(CreateMemberCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var tenantId = request.TenantId;

        var email = string.IsNullOrWhiteSpace(dto.Email)
            ? await MemberEmailGenerator.GenerateAsync(dto.FirstName, dto.LastName, tenantId, db, ct)
            : dto.Email.ToLowerInvariant();

        var existingUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        User user;
        if (existingUser is not null)
        {
            if (await db.Members.AnyAsync(m => m.UserId == existingUser.Id && m.TenantId == tenantId, ct))
                return ServiceResult<MemberDto>.Failure("Cet email est déjà enregistré comme membre.");
            user = existingUser;
        }
        else
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Email = email,
                ContactEmail = dto.ContactEmail?.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Role = "member",
            };
            db.Users.Add(user);
        }

        var seq = await db.Members.CountAsync(m => m.TenantId == tenantId, ct) + 1;
        var membershipNumber = $"MBR-{seq:D4}";

        Guid? categoryId = null;
        if (!string.IsNullOrWhiteSpace(dto.CategoryId) && Guid.TryParse(dto.CategoryId, out var catId))
            categoryId = catId;

        var member = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            MembershipNumber = membershipNumber,
            CategoryId = categoryId,
            Status = "active",
            JoinedDate = DateOnly.Parse(dto.JoinedDate),
            ExpiryDate = dto.ExpiryDate is not null ? DateOnly.Parse(dto.ExpiryDate) : null,
            Address = dto.Address,
            Profession = dto.Profession,
            DateOfBirth = dto.DateOfBirth is not null ? DateOnly.Parse(dto.DateOfBirth) : null,
            Gender = dto.Gender,
            PhotoUrl = dto.PhotoUrl,
            EmergencyContactName = dto.EmergencyContactName,
            EmergencyContactPhone = dto.EmergencyContactPhone,
        };

        db.Members.Add(member);
        await db.SaveChangesAsync(ct);

        log.LogInformation("Member {MembershipNumber} created for tenant {TenantId}", membershipNumber, tenantId);

        if (!string.IsNullOrWhiteSpace(user.ContactEmail))
        {
            var tenant = await db.Tenants.FindAsync([tenantId], ct);
            var baseUrl = config["App:BaseUrl"] ?? "https://app.expatriate365.mu";
            var (plainToken, tokenHash) = server.Application.Common.TokenGenerator.Generate();
            user.ActivationTokenHash      = tokenHash;
            user.ActivationTokenExpiresAt = DateTime.UtcNow.AddHours(72);
            await db.SaveChangesAsync(ct);

            var setPasswordUrl = $"{baseUrl}/set-password?token={plainToken}";
            _ = emailService.SendAsync(
                user.ContactEmail,
                user.FullName,
                $"Activez votre compte — {tenant?.Name ?? "l'association"}",
                EmailTemplates.MemberInvitation(user.FullName, tenant?.Name ?? "", setPasswordUrl),
                ct);
        }

        var category = categoryId.HasValue
            ? await db.MembershipCategories.FindAsync([categoryId.Value], ct)
            : null;

        return ServiceResult<MemberDto>.Success(new MemberDto(
            member.Id.ToString(), tenantId.ToString(), user.Id.ToString(),
            membershipNumber, user.FirstName, user.LastName, user.Email, user.ContactEmail, user.Phone,
            member.Status, categoryId?.ToString(), category?.Name,
            member.JoinedDate.ToString("yyyy-MM-dd"), member.ExpiryDate?.ToString("yyyy-MM-dd"),
            member.PhotoUrl, member.Address, member.Profession,
            member.DateOfBirth?.ToString("yyyy-MM-dd"),
            member.Gender, member.EmergencyContactName, member.EmergencyContactPhone,
            member.IsActive, member.IsDirectoryVisible, member.CreatedAt.ToString("O"), null, user.EmailVerifiedAt?.ToString("O"), user.Role
        ));
    }
}
