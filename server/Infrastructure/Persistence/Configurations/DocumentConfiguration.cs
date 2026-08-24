using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.ToTable("documents");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        b.Property(e => e.Description).HasColumnName("description");
        b.Property(e => e.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
        b.Property(e => e.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        b.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
        b.Property(e => e.FileUrl).HasColumnName("file_url").HasMaxLength(2000).IsRequired();
        b.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
        b.Property(e => e.MimeType).HasColumnName("mime_type").HasMaxLength(200).IsRequired();
        b.Property(e => e.IsPublic).HasColumnName("is_public").HasDefaultValue(true);
        b.Property(e => e.UploadedBy).HasColumnName("uploaded_by").IsRequired();
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(e => e.TenantId);
        b.HasIndex(e => e.Type);
        b.HasOne(e => e.Uploader).WithMany().HasForeignKey(e => e.UploadedBy);
    }
}
