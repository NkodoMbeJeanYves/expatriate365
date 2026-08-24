using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Members.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Members.Commands;

public record UpdateMemberCommand(Guid TenantId, Guid MemberId, UpdateMemberRequest Dto)
    : IRequest<ServiceResult<MemberDto>>;

public class UpdateMemberCommandValidator : AbstractValidator<UpdateMemberCommand>
{
    public UpdateMemberCommandValidator()
    {
        RuleFor(x => x.Dto.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress();
    }
}

public class UpdateMemberCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateMemberCommand, ServiceResult<MemberDto>>
{
    public async Task<ServiceResult<MemberDto>> Handle(UpdateMemberCommand request, CancellationToken ct)
    {
        var member = await db.Members
            .Include(m => m.User)
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == request.MemberId && m.TenantId == request.TenantId, ct);

        if (member is null) return ServiceResult<MemberDto>.Failure("Membre introuvable.");

        var dto = request.Dto;
        member.User.FirstName = dto.FirstName;
        member.User.LastName = dto.LastName;
        member.User.Email = dto.Email.ToLowerInvariant();
        member.User.Phone = dto.Phone;
        member.CategoryId = !string.IsNullOrWhiteSpace(dto.CategoryId) && Guid.TryParse(dto.CategoryId, out var catId) ? catId : null;
        member.ExpiryDate = dto.ExpiryDate is not null ? DateOnly.Parse(dto.ExpiryDate) : null;
        if (dto.PhotoUrl is not null) member.PhotoUrl = dto.PhotoUrl;
        member.Address = dto.Address;
        member.Profession = dto.Profession;
        member.DateOfBirth = dto.DateOfBirth is not null ? DateOnly.Parse(dto.DateOfBirth) : null;
        member.Gender = dto.Gender;
        member.EmergencyContactName = dto.EmergencyContactName;
        member.EmergencyContactPhone = dto.EmergencyContactPhone;
        member.IsActive = dto.IsActive;

        await db.SaveChangesAsync(ct);

        var category = member.CategoryId.HasValue
            ? await db.MembershipCategories.FindAsync([member.CategoryId.Value], ct)
            : null;

        return ServiceResult<MemberDto>.Success(new MemberDto(
            member.Id.ToString(), member.TenantId.ToString(), member.UserId.ToString(),
            member.MembershipNumber, member.User.FirstName, member.User.LastName,
            member.User.Email, member.User.Phone, member.Status,
            member.CategoryId?.ToString(), category?.Name,
            member.JoinedDate.ToString("yyyy-MM-dd"), member.ExpiryDate?.ToString("yyyy-MM-dd"),
            member.PhotoUrl, member.Address, member.Profession,
            member.DateOfBirth?.ToString("yyyy-MM-dd"),
            member.Gender, member.EmergencyContactName, member.EmergencyContactPhone,
            member.IsActive, member.CreatedAt.ToString("O"), member.UpdatedAt?.ToString("O"),
            member.User.EmailVerifiedAt?.ToString("O"), member.User.Role
        ));
    }
}
