using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
{
    public void Configure(EntityTypeBuilder<EventRegistration> b)
    {
        b.ToTable("event_registrations");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.EventId).HasColumnName("event_id");
        b.Property(e => e.MemberId).HasColumnName("member_id");
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
        b.Property(e => e.AttendedAt).HasColumnName("attended_at");
        b.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(1000);
        b.Property(e => e.IsActive).HasColumnName("is_active");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasOne(e => e.Event).WithMany(ev => ev.Registrations).HasForeignKey(e => e.EventId);
        b.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId);

        b.HasIndex(e => new { e.EventId, e.MemberId }).IsUnique();
    }
}
