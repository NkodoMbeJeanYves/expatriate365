using MediatR;
using server.Application.Common;
using server.Application.Communications.DTOs;
using server.Application.Communications.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Communications.Commands;

public record CreateCommunicationCommand(Guid TenantId, CreateCommunicationRequest Request)
    : IRequest<ServiceResult<CommunicationDto>>;

public class CreateCommunicationCommandHandler(AppDbContext db)
    : IRequestHandler<CreateCommunicationCommand, ServiceResult<CommunicationDto>>
{
    public async Task<ServiceResult<CommunicationDto>> Handle(CreateCommunicationCommand request, CancellationToken ct)
    {
        var req = request.Request;

        var comm = new Communication
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Title = req.Title,
            Content = req.Content,
            Type = req.Type,
            Channel = req.Channel,
            Audience = req.Audience,
            CategoryId = req.CategoryId is not null ? Guid.Parse(req.CategoryId) : null,
            TargetMemberId = req.TargetMemberId is not null ? Guid.Parse(req.TargetMemberId) : null,
            Status = "draft",
        };

        db.Communications.Add(comm);
        await db.SaveChangesAsync(ct);

        return ServiceResult<CommunicationDto>.Success(ListCommunicationsQueryHandler.ToDto(comm));
    }
}
