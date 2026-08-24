using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Documents.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Documents.Queries;

public record ListDocumentsQuery(
    Guid TenantId, int Page, int Limit, string? Type, string? Category, string? Search
) : IRequest<PagedResult<DocumentDto>>;

public class ListDocumentsQueryHandler(AppDbContext db)
    : IRequestHandler<ListDocumentsQuery, PagedResult<DocumentDto>>
{
    public async Task<PagedResult<DocumentDto>> Handle(ListDocumentsQuery request, CancellationToken ct)
    {
        var query = db.Documents
            .Include(d => d.Uploader)
            .Where(d => d.TenantId == request.TenantId && d.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Type))
            query = query.Where(d => d.Type == request.Type);
        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(d => d.Category == request.Category);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(d => d.Title.Contains(request.Search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(ct);

        return PagedResult<DocumentDto>.Create(items.Select(ToDto).ToList(), request.Page, request.Limit, total);
    }

    internal static DocumentDto ToDto(Domain.Entities.Document d) => new(
        d.Id.ToString(), d.TenantId.ToString(), d.Title, d.Description,
        d.Type, d.Category, d.FileName, d.FileUrl, d.FileSizeBytes, d.MimeType, d.IsPublic,
        d.UploadedBy.ToString(), $"{d.Uploader.FirstName} {d.Uploader.LastName}",
        d.CreatedAt.ToString("O"), d.UpdatedAt?.ToString("O"));
}
