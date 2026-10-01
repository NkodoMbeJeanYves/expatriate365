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
        var docs = await db.Documents
            .Where(d => d.TenantId == request.TenantId && d.IsActive)
            .Select(d => new { d.IsPublic, d.Category })
            .ToListAsync(ct);

        var total   = docs.Count;
        var pub     = docs.Count(d => d.IsPublic);
        var priv    = docs.Count(d => !d.IsPublic);
        var byCategory = docs
            .GroupBy(d => d.Category)
            .Select(g => new DocumentCategoryStatDto(g.Key, g.Count()))
            .OrderByDescending(c => c.Count)
            .ToList();

        return new DocumentStatsDto(total, pub, priv, byCategory);
    }
}
