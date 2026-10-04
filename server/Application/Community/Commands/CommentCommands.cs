using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Community.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Community.Commands;

// ── Add Comment ───────────────────────────────────────────────────────────────

public record AddCommentCommand(Guid TenantId, Guid PostId, Guid AuthorMemberId, string Content)
    : IRequest<ServiceResult<PostCommentDto>>;

public class AddCommentHandler(AppDbContext db, ILogger<AddCommentHandler> log)
    : IRequestHandler<AddCommentCommand, ServiceResult<PostCommentDto>>
{
    public async Task<ServiceResult<PostCommentDto>> Handle(AddCommentCommand request, CancellationToken ct)
    {
        var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId && p.TenantId == request.TenantId && p.IsActive, ct);
        if (post is null) return ServiceResult<PostCommentDto>.Failure("Post not found.", "errors.post.not_found");

        var author = await db.Members.Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == request.AuthorMemberId && m.TenantId == request.TenantId && m.IsActive, ct);
        if (author is null) return ServiceResult<PostCommentDto>.Failure("Member not found.", "errors.member.not_found");

        var comment = new PostComment
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            PostId = request.PostId,
            AuthorMemberId = request.AuthorMemberId,
            Content = request.Content.Trim(),
        };

        db.PostComments.Add(comment);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Comment added {Id} on post {PostId}", comment.Id, request.PostId);

        return ServiceResult<PostCommentDto>.Success(new PostCommentDto(
            comment.Id.ToString(), comment.PostId.ToString(),
            comment.AuthorMemberId.ToString(),
            author.User.FirstName + " " + author.User.LastName,
            comment.Content, comment.CreatedAt.ToString("O"), null));
    }
}

// ── Delete Comment ────────────────────────────────────────────────────────────

public record DeleteCommentCommand(Guid TenantId, Guid CommentId, Guid RequesterId, bool IsStaff)
    : IRequest<ServiceResult<bool>>;

public class DeleteCommentHandler(AppDbContext db, ILogger<DeleteCommentHandler> log)
    : IRequestHandler<DeleteCommentCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        var comment = await db.PostComments.FirstOrDefaultAsync(
            c => c.Id == request.CommentId && c.TenantId == request.TenantId && c.IsActive, ct);
        if (comment is null) return ServiceResult<bool>.Failure("Comment not found.", "errors.comment.not_found");

        if (!request.IsStaff && comment.AuthorMemberId != request.RequesterId)
            return ServiceResult<bool>.Failure("Not authorized to delete this comment.", "errors.comment.not_authorized");

        comment.IsActive = false;
        comment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Comment deleted {Id}", request.CommentId);

        return ServiceResult<bool>.Success(true);
    }
}

// ── Toggle Reaction ───────────────────────────────────────────────────────────

public record ToggleReactionCommand(Guid TenantId, Guid PostId, Guid MemberId, string ReactionType)
    : IRequest<ServiceResult<PostReactionCountDto>>;

public class ToggleReactionHandler(AppDbContext db, ILogger<ToggleReactionHandler> log)
    : IRequestHandler<ToggleReactionCommand, ServiceResult<PostReactionCountDto>>
{
    public async Task<ServiceResult<PostReactionCountDto>> Handle(ToggleReactionCommand request, CancellationToken ct)
    {
        var existing = await db.PostReactions.FirstOrDefaultAsync(
            r => r.PostId == request.PostId && r.MemberId == request.MemberId && r.ReactionType == request.ReactionType, ct);

        bool userReacted;
        if (existing is not null)
        {
            db.PostReactions.Remove(existing);
            userReacted = false;
            log.LogInformation("Reaction removed {Type} on post {PostId}", request.ReactionType, request.PostId);
        }
        else
        {
            db.PostReactions.Add(new PostReaction
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                PostId = request.PostId,
                MemberId = request.MemberId,
                ReactionType = request.ReactionType,
            });
            userReacted = true;
            log.LogInformation("Reaction added {Type} on post {PostId}", request.ReactionType, request.PostId);
        }

        await db.SaveChangesAsync(ct);

        var count = await db.PostReactions.CountAsync(
            r => r.PostId == request.PostId && r.ReactionType == request.ReactionType, ct);

        return ServiceResult<PostReactionCountDto>.Success(new PostReactionCountDto(request.ReactionType, count, userReacted));
    }
}
