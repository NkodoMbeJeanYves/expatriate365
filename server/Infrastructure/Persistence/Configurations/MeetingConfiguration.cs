using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> b)
    {
        b.ToTable("meetings");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        b.Property(e => e.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        b.Property(e => e.ScheduledAt).HasColumnName("scheduled_at").IsRequired();
        b.Property(e => e.Location).HasColumnName("location").HasMaxLength(300);
        b.Property(e => e.Agenda).HasColumnName("agenda");
        b.Property(e => e.QuorumRequired).HasColumnName("quorum_required");
        b.Property(e => e.StartedAt).HasColumnName("started_at");
        b.Property(e => e.EndedAt).HasColumnName("ended_at");
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(e => e.TenantId);
        b.HasIndex(e => e.Status);
    }
}

public class MeetingAttendanceConfiguration : IEntityTypeConfiguration<MeetingAttendance>
{
    public void Configure(EntityTypeBuilder<MeetingAttendance> b)
    {
        b.ToTable("meeting_attendances");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.MeetingId).HasColumnName("meeting_id").IsRequired();
        b.Property(e => e.MemberId).HasColumnName("member_id").IsRequired();
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        b.Property(e => e.ProxyName).HasColumnName("proxy_name").HasMaxLength(200);
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(e => new { e.MeetingId, e.MemberId }).IsUnique();

        b.HasOne(e => e.Meeting).WithMany(m => m.Attendances).HasForeignKey(e => e.MeetingId);
        b.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId);
    }
}

public class MeetingMinuteConfiguration : IEntityTypeConfiguration<MeetingMinute>
{
    public void Configure(EntityTypeBuilder<MeetingMinute> b)
    {
        b.ToTable("meeting_minutes");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.MeetingId).HasColumnName("meeting_id").IsRequired();
        b.Property(e => e.Content).HasColumnName("content").IsRequired();
        b.Property(e => e.Decisions).HasColumnName("decisions");
        b.Property(e => e.IsApproved).HasColumnName("is_approved").HasDefaultValue(false);
        b.Property(e => e.ApprovedAt).HasColumnName("approved_at");
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(e => e.MeetingId).IsUnique();
        b.HasOne(e => e.Meeting).WithOne(m => m.Minute).HasForeignKey<MeetingMinute>(e => e.MeetingId);
    }
}
