using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Communications.DTOs;
using server.Application.Communications.Queries;
using server.Infrastructure.Persistence;

namespace server.Application.Communications.Commands;

public record UpdateCommunicationCommand(Guid TenantId, Guid Id, UpdateCommunicationRequest Request)
    : IRequest<ServiceResult<CommunicationDto>>;

public class UpdateCommunicationCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateCommunicationCommand, ServiceResult<CommunicationDto>>
{
    public async Task<ServiceResult<CommunicationDto>> Handle(UpdateCommunicationCommand request, CancellationToken ct)
    {
        var comm = await db.Communications
            .Include(c => c.Recipients)
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == request.TenantId, ct);

        if (comm is null)
            return ServiceResult<CommunicationDto>.Failure("Communication introuvable.");

        if (comm.Status == "sent")
            return ServiceResult<CommunicationDto>.Failure("Une communication déjà envoyée ne peut pas être modifiée.");

        var req = request.Request;
        comm.Title = req.Title;
        comm.Content = req.Content;
        comm.Type = req.Type;
        comm.Channel = req.Channel;
        comm.Audience = req.Audience;
        comm.CategoryId = req.CategoryId is not null ? Guid.Parse(req.CategoryId) : null;
        comm.TargetMemberId = req.TargetMemberId is not null ? Guid.Parse(req.TargetMemberId) : null;

        await db.SaveChangesAsync(ct);
        return ServiceResult<CommunicationDto>.Success(ListCommunicationsQueryHandler.ToDto(comm));
    }
}
