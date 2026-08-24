using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Communications.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Communications.Queries;

public record GetCommunicationByIdQuery(Guid TenantId, Guid Id)
    : IRequest<ServiceResult<(CommunicationDto Communication, List<RecipientDto> Recipients)>>;

public class GetCommunicationByIdQueryHandler(AppDbContext db)
    : IRequestHandler<GetCommunicationByIdQuery, ServiceResult<(CommunicationDto, List<RecipientDto>)>>
{
    public async Task<ServiceResult<(CommunicationDto, List<RecipientDto>)>> Handle(
        GetCommunicationByIdQuery request, CancellationToken ct)
    {
        var c = await db.Communications
            .Include(x => x.Recipients.Where(r => r.IsActive))
                .ThenInclude(r => r.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (c is null)
            return ServiceResult<(CommunicationDto, List<RecipientDto>)>.Failure("Communication introuvable.");

        var recipients = c.Recipients.Select(r => new RecipientDto(
            r.Id.ToString(), r.MemberId.ToString(),
            $"{r.Member.User.FirstName} {r.Member.User.LastName}",
            r.Member.MembershipNumber, r.Status,
            r.ReadAt?.ToString("O"), r.CreatedAt.ToString("O"))).ToList();

        return ServiceResult<(CommunicationDto, List<RecipientDto>)>
            .Success((ListCommunicationsQueryHandler.ToDto(c), recipients));
    }
}
