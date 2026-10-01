using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Community.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Community.Queries;

public record ListCommentsQuery(Guid TenantId, Guid PostId) : IRequest<List<PostCommentDto>>;

public class ListCommentsQueryHandler(AppDbContext db)
    : IRequestHandler<ListCommentsQuery, List<PostCommentDto>>
{
    public async Task<List<PostCommentDto>> Handle(ListCommentsQuery request, CancellationToken ct)
    {
        var comments = await db.PostComments
            .AsNoTracking()
            .Include(c => c.Author).ThenInclude(m => m.User)
            .Where(c => c.TenantId == request.TenantId && c.PostId == request.PostId && c.IsActive)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        return comments.Select(c => new PostCommentDto(
            c.Id.ToString(), c.PostId.ToString(),
            c.AuthorMemberId.ToString(),
            c.Author.User.FirstName + " " + c.Author.User.LastName,
            c.Content, c.CreatedAt.ToString("O"), c.UpdatedAt?.ToString("O")
        )).ToList();
    }
}

public record GetPostReactionsQuery(Guid TenantId, Guid PostId, Guid? CurrentMemberId) : IRequest<List<PostReactionCountDto>>;

public class GetPostReactionsQueryHandler(AppDbContext db)
    : IRequestHandler<GetPostReactionsQuery, List<PostReactionCountDto>>
{
    public async Task<List<PostReactionCountDto>> Handle(GetPostReactionsQuery request, CancellationToken ct)
    {
        var reactions = await db.PostReactions
            .AsNoTracking()
            .Where(r => r.PostId == request.PostId && r.TenantId == request.TenantId)
            .ToListAsync(ct);

        return reactions
            .GroupBy(r => r.ReactionType)
            .Select(g => new PostReactionCountDto(
                g.Key, g.Count(),
                request.CurrentMemberId.HasValue && g.Any(r => r.MemberId == request.CurrentMemberId.Value)))
            .ToList();
    }
}
