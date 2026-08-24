using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> b)
    {
        b.ToTable("events");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        b.Property(e => e.Description).HasColumnName("description").HasMaxLength(5000);
        b.Property(e => e.Type).HasColumnName("type").HasMaxLength(50);
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
        b.Property(e => e.Location).HasColumnName("location").HasMaxLength(500);
        b.Property(e => e.StartDate).HasColumnName("start_date");
        b.Property(e => e.EndDate).HasColumnName("end_date");
        b.Property(e => e.MaxCapacity).HasColumnName("max_capacity");
        b.Property(e => e.IsPublic).HasColumnName("is_public");
        b.Property(e => e.ImageUrl).HasColumnName("image_url").HasMaxLength(1000);
        b.Property(e => e.IsActive).HasColumnName("is_active");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }
}
