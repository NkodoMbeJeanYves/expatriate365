using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Documents.DTOs;
using server.Application.Documents.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Documents.Commands;

public record CreateDocumentCommand(Guid TenantId, Guid UploadedBy, CreateDocumentRequest Request)
    : IRequest<ServiceResult<DocumentDto>>;

public class CreateDocumentCommandHandler(AppDbContext db)
    : IRequestHandler<CreateDocumentCommand, ServiceResult<DocumentDto>>
{
    public async Task<ServiceResult<DocumentDto>> Handle(CreateDocumentCommand request, CancellationToken ct)
    {
        var req = request.Request;
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Title = req.Title,
            Description = req.Description,
            Type = req.Type,
            Category = req.Category,
            FileName = req.FileName,
            FileUrl = req.FileUrl,
            FileSizeBytes = req.FileSizeBytes,
            MimeType = req.MimeType,
            IsPublic = req.IsPublic,
            UploadedBy = request.UploadedBy,
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync(ct);

        await db.Entry(doc).Reference(d => d.Uploader).LoadAsync(ct);
        return ServiceResult<DocumentDto>.Success(ListDocumentsQueryHandler.ToDto(doc));
    }
}

public record UpdateDocumentCommand(Guid TenantId, Guid Id, UpdateDocumentRequest Request)
    : IRequest<ServiceResult<DocumentDto>>;

public class UpdateDocumentCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateDocumentCommand, ServiceResult<DocumentDto>>
{
    public async Task<ServiceResult<DocumentDto>> Handle(UpdateDocumentCommand request, CancellationToken ct)
    {
        var doc = await db.Documents
            .Include(d => d.Uploader)
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.TenantId == request.TenantId, ct);

        if (doc is null) return ServiceResult<DocumentDto>.Failure("Document introuvable.");

        var req = request.Request;
        doc.Title = req.Title;
        doc.Description = req.Description;
        doc.Type = req.Type;
        doc.Category = req.Category;
        doc.IsPublic = req.IsPublic;
        await db.SaveChangesAsync(ct);

        return ServiceResult<DocumentDto>.Success(ListDocumentsQueryHandler.ToDto(doc));
    }
}

public record DeleteDocumentCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<bool>>;

public class DeleteDocumentCommandHandler(AppDbContext db)
    : IRequestHandler<DeleteDocumentCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeleteDocumentCommand request, CancellationToken ct)
    {
        var doc = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.TenantId == request.TenantId, ct);

        if (doc is null) return ServiceResult<bool>.Failure("Document introuvable.");
        doc.IsActive = false;
        await db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }
}
