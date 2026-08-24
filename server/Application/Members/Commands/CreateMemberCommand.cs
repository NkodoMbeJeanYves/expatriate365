using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Members.DTOs;
using server.Application.Members.Services;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

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

public class CreateMemberCommandHandler(AppDbContext db, ILogger<CreateMemberCommandHandler> log)
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

        var category = categoryId.HasValue
            ? await db.MembershipCategories.FindAsync([categoryId.Value], ct)
            : null;

        return ServiceResult<MemberDto>.Success(new MemberDto(
            member.Id.ToString(), tenantId.ToString(), user.Id.ToString(),
            membershipNumber, user.FirstName, user.LastName, user.Email, user.Phone,
            member.Status, categoryId?.ToString(), category?.Name,
            member.JoinedDate.ToString("yyyy-MM-dd"), member.ExpiryDate?.ToString("yyyy-MM-dd"),
            member.PhotoUrl, member.Address, member.Profession,
            member.DateOfBirth?.ToString("yyyy-MM-dd"),
            member.Gender, member.EmergencyContactName, member.EmergencyContactPhone,
            member.IsActive, member.CreatedAt.ToString("O"), null, user.EmailVerifiedAt?.ToString("O"), user.Role
        ));
    }
}
