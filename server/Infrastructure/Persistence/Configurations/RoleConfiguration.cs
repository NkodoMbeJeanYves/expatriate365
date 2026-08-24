using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Label).HasColumnName("label").HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(r => r.Permissions).HasColumnName("permissions").HasMaxLength(2000).HasDefaultValue("[]");
        builder.Property(r => r.IsActive).HasColumnName("is_active");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(r => r.Name).IsUnique();
    }
}
