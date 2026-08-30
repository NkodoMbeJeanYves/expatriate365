using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class PostAttachmentConfiguration : IEntityTypeConfiguration<PostAttachment>
{
    public void Configure(EntityTypeBuilder<PostAttachment> b)
    {
        b.ToTable("post_attachments");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.PostId).HasColumnName("post_id").IsRequired();
        b.Property(e => e.FileUrl).HasColumnName("file_url").HasMaxLength(2000).IsRequired();
        b.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
        b.Property(e => e.MimeType).HasColumnName("mime_type").HasMaxLength(200).IsRequired();
        b.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
        b.Property(e => e.AttachmentType).HasColumnName("attachment_type").HasMaxLength(20).HasDefaultValue("document");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");

        b.HasIndex(e => e.PostId);
    }
}
