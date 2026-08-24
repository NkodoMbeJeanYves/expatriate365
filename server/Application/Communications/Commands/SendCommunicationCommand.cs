using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Communications.DTOs;
using server.Application.Communications.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Communications.Commands;

public record SendCommunicationCommand(Guid TenantId, Guid Id)
    : IRequest<ServiceResult<CommunicationDto>>;

public class SendCommunicationCommandHandler(AppDbContext db)
    : IRequestHandler<SendCommunicationCommand, ServiceResult<CommunicationDto>>
{
    public async Task<ServiceResult<CommunicationDto>> Handle(SendCommunicationCommand request, CancellationToken ct)
    {
        var comm = await db.Communications
            .Include(c => c.Recipients)
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == request.TenantId, ct);

        if (comm is null)
            return ServiceResult<CommunicationDto>.Failure("Communication introuvable.");
        if (comm.Status == "sent")
            return ServiceResult<CommunicationDto>.Failure("Communication déjà envoyée.");

        var memberIds = await ResolveRecipientsAsync(comm, request.TenantId, ct);

        var existingIds = comm.Recipients.Select(r => r.MemberId).ToHashSet();
        var newRecipients = memberIds
            .Where(id => !existingIds.Contains(id))
            .Select(id => new CommunicationRecipient
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                CommunicationId = comm.Id,
                MemberId = id,
                Status = "sent",
            })
            .ToList();

        db.CommunicationRecipients.AddRange(newRecipients);

        comm.Status = "sent";
        comm.SentAt = DateTime.UtcNow;
        comm.RecipientCount = existingIds.Count + newRecipients.Count;

        await db.SaveChangesAsync(ct);
        return ServiceResult<CommunicationDto>.Success(ListCommunicationsQueryHandler.ToDto(comm));
    }

    private async Task<List<Guid>> ResolveRecipientsAsync(Communication comm, Guid tenantId, CancellationToken ct)
    {
        var query = db.Members.Where(m => m.TenantId == tenantId && m.IsActive && m.Status == "active");

        return comm.Audience switch
        {
            "individual" when comm.TargetMemberId is not null =>
                [comm.TargetMemberId.Value],

            "category" when comm.CategoryId is not null =>
                await query.Where(m => m.CategoryId == comm.CategoryId)
                           .Select(m => m.Id).ToListAsync(ct),

            _ => await query.Select(m => m.Id).ToListAsync(ct),
        };
    }
}
