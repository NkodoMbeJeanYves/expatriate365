using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Documents.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Documents.Queries;

public record GetDocumentStatsQuery(Guid TenantId) : IRequest<DocumentStatsDto>;

public class GetDocumentStatsQueryHandler(AppDbContext db)
    : IRequestHandler<GetDocumentStatsQuery, DocumentStatsDto>
{
    public async Task<DocumentStatsDto> Handle(GetDocumentStatsQuery request, CancellationToken ct)
    {
        var stats = await db.Documents
            .Where(d => d.TenantId == request.TenantId && d.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Public = g.Count(d => d.IsPublic),
                Private = g.Count(d => !d.IsPublic),
            })
            .FirstOrDefaultAsync(ct);

        return stats is null
            ? new DocumentStatsDto(0, 0, 0)
            : new DocumentStatsDto(stats.Total, stats.Public, stats.Private);
    }
}
