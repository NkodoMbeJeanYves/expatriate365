using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Community.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Community.Queries;

public record ListPostsQuery(Guid TenantId, int Page, int Limit, string? Status, string? AuthorId, string? Search)
    : IRequest<PagedResult<PostSummaryDto>>;

public class ListPostsQueryHandler(AppDbContext db)
    : IRequestHandler<ListPostsQuery, PagedResult<PostSummaryDto>>
{
    public async Task<PagedResult<PostSummaryDto>> Handle(ListPostsQuery request, CancellationToken ct)
    {
        var q = db.Posts
            .Include(p => p.Author).ThenInclude(m => m.User)
            .Include(p => p.Attachments)
            .Where(p => p.TenantId == request.TenantId && p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Status))
            q = q.Where(p => p.Status == request.Status);

        if (Guid.TryParse(request.AuthorId, out var authorGuid))
            q = q.Where(p => p.AuthorId == authorGuid);

        if (!string.IsNullOrWhiteSpace(request.Search))
            q = q.Where(p => p.Title.Contains(request.Search) || p.Content.Contains(request.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .Include(p => p.Author).ThenInclude(m => m.User)
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(ct);

        return new PagedResult<PostSummaryDto>
        {
            Data       = items.Select(ToSummaryDto),
            Pagination = new PaginationMeta { Page = request.Page, Limit = request.Limit, Total = total },
        };
    }

    internal static PostSummaryDto ToSummaryDto(Post p) => new(
        p.Id.ToString(), p.AuthorId.ToString(),
        $"{p.Author.User.FullName}".Trim(),
        p.Title,
        p.Content.Length > 200 ? p.Content[..200] + "…" : p.Content,
        p.Status,
        p.PublishedAt?.ToString("O"),
        p.CreatedAt.ToString("O"),
        p.Attachments.Count);

    internal static PostDto ToDto(Post p) => new(
        p.Id.ToString(), p.TenantId.ToString(), p.AuthorId.ToString(),
        $"{p.Author.User.FullName}".Trim(),
        p.Title, p.Content, p.Status,
        p.PublishedAt?.ToString("O"),
        p.CreatedAt.ToString("O"),
        p.UpdatedAt?.ToString("O"),
        p.Attachments.Select(a => new PostAttachmentDto(
            a.Id.ToString(), a.PostId.ToString(), a.FileUrl, a.FileName,
            a.MimeType, a.FileSizeBytes, a.AttachmentType, a.CreatedAt.ToString("O"))));
}

public record GetPostQuery(Guid TenantId, Guid PostId) : IRequest<PostDto?>;

public class GetPostQueryHandler(AppDbContext db)
    : IRequestHandler<GetPostQuery, PostDto?>
{
    public async Task<PostDto?> Handle(GetPostQuery request, CancellationToken ct)
    {
        var post = await db.Posts
            .Include(p => p.Author).ThenInclude(m => m.User)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == request.PostId && p.TenantId == request.TenantId && p.IsActive, ct);

        return post is null ? null : ListPostsQueryHandler.ToDto(post);
    }
}
