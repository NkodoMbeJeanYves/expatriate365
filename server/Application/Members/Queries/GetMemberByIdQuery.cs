using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Members.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Members.Queries;

public record GetMemberByIdQuery(Guid TenantId, Guid MemberId) : IRequest<ServiceResult<MemberDto>>;

public class GetMemberByIdQueryHandler(AppDbContext db)
    : IRequestHandler<GetMemberByIdQuery, ServiceResult<MemberDto>>
{
    public async Task<ServiceResult<MemberDto>> Handle(GetMemberByIdQuery q, CancellationToken ct)
    {
        var m = await db.Members
            .Include(x => x.User)
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == q.MemberId && x.TenantId == q.TenantId, ct);

        if (m is null) return ServiceResult<MemberDto>.Failure("Membre introuvable.");

        return ServiceResult<MemberDto>.Success(new MemberDto(
            m.Id.ToString(), m.TenantId.ToString(), m.UserId.ToString(),
            m.MembershipNumber, m.User.FirstName, m.User.LastName, m.User.Email, m.User.Phone,
            m.Status,
            m.CategoryId?.ToString(), m.Category?.Name,
            m.JoinedDate.ToString("yyyy-MM-dd"),
            m.ExpiryDate?.ToString("yyyy-MM-dd"),
            m.PhotoUrl, m.Address, m.Profession,
            m.DateOfBirth?.ToString("yyyy-MM-dd"),
            m.Gender, m.EmergencyContactName, m.EmergencyContactPhone,
            m.IsActive, m.CreatedAt.ToString("O"), m.UpdatedAt?.ToString("O"),
            m.User.EmailVerifiedAt?.ToString("O"), m.User.Role
        ));
    }
}
