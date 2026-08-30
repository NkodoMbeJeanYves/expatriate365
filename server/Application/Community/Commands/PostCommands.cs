using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Community.DTOs;
using server.Application.Community.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Community.Commands;

public record CreatePostCommand(Guid TenantId, Guid AuthorId, Guid UserId, CreatePostRequest Request)
    : IRequest<ServiceResult<PostDto>>;

public class CreatePostCommandHandler(AppDbContext db)
    : IRequestHandler<CreatePostCommand, ServiceResult<PostDto>>
{
    public async Task<ServiceResult<PostDto>> Handle(CreatePostCommand request, CancellationToken ct)
    {
        // Resolve the member — entity_id may be the user UUID when the token was issued
        // before a member record existed (e.g. super_admin acting on a tenant)
        var memberId = request.AuthorId;
        var memberExists = await db.Members.AnyAsync(
            m => m.Id == memberId && m.TenantId == request.TenantId && m.IsActive, ct);

        if (!memberExists)
        {
            var member = await db.Members.FirstOrDefaultAsync(
                m => m.UserId == request.UserId && m.TenantId == request.TenantId && m.IsActive, ct);
            if (member is null)
                return ServiceResult<PostDto>.Failure("Vous devez être membre pour publier une expérience.");
            memberId = member.Id;
        }

        var post = new Post
        {
            Id       = Guid.NewGuid(),
            TenantId = request.TenantId,
            AuthorId = memberId,
            Title    = request.Request.Title,
            Content  = request.Request.Content,
            Status   = "draft",
        };
        db.Posts.Add(post);
        await db.SaveChangesAsync(ct);
        await db.Entry(post).Reference(p => p.Author).Query().Include(m => m.User).LoadAsync(ct);
        return ServiceResult<PostDto>.Success(ListPostsQueryHandler.ToDto(post));
    }
}

public record UpdatePostCommand(Guid TenantId, Guid PostId, Guid RequesterId, UpdatePostRequest Request)
    : IRequest<ServiceResult<PostDto>>;

public class UpdatePostCommandHandler(AppDbContext db)
    : IRequestHandler<UpdatePostCommand, ServiceResult<PostDto>>
{
    public async Task<ServiceResult<PostDto>> Handle(UpdatePostCommand request, CancellationToken ct)
    {
        var post = await db.Posts
            .Include(p => p.Author).ThenInclude(m => m.User)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == request.PostId && p.TenantId == request.TenantId, ct);

        if (post is null) return ServiceResult<PostDto>.Failure("Publication introuvable.");
        if (post.AuthorId != request.RequesterId)
            return ServiceResult<PostDto>.Failure("Vous ne pouvez modifier que vos propres publications.");
        if (post.Status == "published")
            return ServiceResult<PostDto>.Failure("Une publication publiée ne peut pas être modifiée.");

        post.Title   = request.Request.Title;
        post.Content = request.Request.Content;
        await db.SaveChangesAsync(ct);
        return ServiceResult<PostDto>.Success(ListPostsQueryHandler.ToDto(post));
    }
}

public record DeletePostCommand(Guid TenantId, Guid PostId, Guid RequesterId, bool IsStaff)
    : IRequest<ServiceResult<bool>>;

public class DeletePostCommandHandler(AppDbContext db)
    : IRequestHandler<DeletePostCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeletePostCommand request, CancellationToken ct)
    {
        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && p.TenantId == request.TenantId, ct);

        if (post is null) return ServiceResult<bool>.Failure("Publication introuvable.");
        if (!request.IsStaff && post.AuthorId != request.RequesterId)
            return ServiceResult<bool>.Failure("Action non autorisée.");

        post.IsActive = false;
        await db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }
}

public record PublishPostCommand(Guid TenantId, Guid PostId) : IRequest<ServiceResult<PostDto>>;

public class PublishPostCommandHandler(AppDbContext db)
    : IRequestHandler<PublishPostCommand, ServiceResult<PostDto>>
{
    public async Task<ServiceResult<PostDto>> Handle(PublishPostCommand request, CancellationToken ct)
    {
        var post = await db.Posts
            .Include(p => p.Author).ThenInclude(m => m.User)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == request.PostId && p.TenantId == request.TenantId, ct);

        if (post is null) return ServiceResult<PostDto>.Failure("Publication introuvable.");
        if (post.Status == "published") return ServiceResult<PostDto>.Failure("Déjà publiée.");

        post.Status      = "published";
        post.PublishedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ServiceResult<PostDto>.Success(ListPostsQueryHandler.ToDto(post));
    }
}

public record RejectPostCommand(Guid TenantId, Guid PostId) : IRequest<ServiceResult<PostDto>>;

public class RejectPostCommandHandler(AppDbContext db)
    : IRequestHandler<RejectPostCommand, ServiceResult<PostDto>>
{
    public async Task<ServiceResult<PostDto>> Handle(RejectPostCommand request, CancellationToken ct)
    {
        var post = await db.Posts
            .Include(p => p.Author).ThenInclude(m => m.User)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == request.PostId && p.TenantId == request.TenantId, ct);

        if (post is null) return ServiceResult<PostDto>.Failure("Publication introuvable.");

        post.Status = "rejected";
        await db.SaveChangesAsync(ct);
        return ServiceResult<PostDto>.Success(ListPostsQueryHandler.ToDto(post));
    }
}

public record AddAttachmentCommand(Guid TenantId, Guid PostId, Guid RequesterId, AddAttachmentRequest Request)
    : IRequest<ServiceResult<PostAttachmentDto>>;

public class AddAttachmentCommandHandler(AppDbContext db)
    : IRequestHandler<AddAttachmentCommand, ServiceResult<PostAttachmentDto>>
{
    public async Task<ServiceResult<PostAttachmentDto>> Handle(AddAttachmentCommand request, CancellationToken ct)
    {
        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && p.TenantId == request.TenantId, ct);

        if (post is null) return ServiceResult<PostAttachmentDto>.Failure("Publication introuvable.");
        if (post.AuthorId != request.RequesterId)
            return ServiceResult<PostAttachmentDto>.Failure("Action non autorisée.");

        var att = new PostAttachment
        {
            Id             = Guid.NewGuid(),
            PostId         = post.Id,
            FileUrl        = request.Request.FileUrl,
            FileName       = request.Request.FileName,
            MimeType       = request.Request.MimeType,
            FileSizeBytes  = request.Request.FileSizeBytes,
            AttachmentType = request.Request.AttachmentType,
        };
        db.PostAttachments.Add(att);
        await db.SaveChangesAsync(ct);
        return ServiceResult<PostAttachmentDto>.Success(ToAttachmentDto(att));
    }

    internal static PostAttachmentDto ToAttachmentDto(PostAttachment a) => new(
        a.Id.ToString(), a.PostId.ToString(), a.FileUrl, a.FileName,
        a.MimeType, a.FileSizeBytes, a.AttachmentType,
        a.CreatedAt.ToString("O"));
}

public record DeleteAttachmentCommand(Guid TenantId, Guid PostId, Guid AttachmentId, Guid RequesterId)
    : IRequest<ServiceResult<bool>>;

public class DeleteAttachmentCommandHandler(AppDbContext db)
    : IRequestHandler<DeleteAttachmentCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeleteAttachmentCommand request, CancellationToken ct)
    {
        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && p.TenantId == request.TenantId, ct);

        if (post is null) return ServiceResult<bool>.Failure("Publication introuvable.");
        if (post.AuthorId != request.RequesterId)
            return ServiceResult<bool>.Failure("Action non autorisée.");

        var att = await db.PostAttachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId && a.PostId == request.PostId, ct);

        if (att is null) return ServiceResult<bool>.Failure("Pièce jointe introuvable.");
        db.PostAttachments.Remove(att);
        await db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }
}
