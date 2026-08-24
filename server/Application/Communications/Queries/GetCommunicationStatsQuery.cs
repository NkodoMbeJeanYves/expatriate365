using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Communications.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Communications.Queries;

public record GetCommunicationStatsQuery(Guid TenantId) : IRequest<CommunicationStatsDto>;

public class GetCommunicationStatsQueryHandler(AppDbContext db)
    : IRequestHandler<GetCommunicationStatsQuery, CommunicationStatsDto>
{
    public async Task<CommunicationStatsDto> Handle(GetCommunicationStatsQuery request, CancellationToken ct)
    {
        var comms = await db.Communications
            .Where(c => c.TenantId == request.TenantId && c.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Draft = g.Count(c => c.Status == "draft"),
                Sent = g.Count(c => c.Status == "sent"),
                TotalRecipients = g.Sum(c => c.RecipientCount),
            })
            .FirstOrDefaultAsync(ct);

        var readCount = await db.CommunicationRecipients
            .Where(r => r.TenantId == request.TenantId && r.Status == "read" && r.IsActive)
            .CountAsync(ct);

        return comms is null
            ? new CommunicationStatsDto(0, 0, 0, 0, 0)
            : new CommunicationStatsDto(comms.Total, comms.Draft, comms.Sent, comms.TotalRecipients, readCount);
    }
}
