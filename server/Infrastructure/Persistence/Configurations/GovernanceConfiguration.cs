using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class BoardMemberConfiguration : IEntityTypeConfiguration<BoardMember>
{
    public void Configure(EntityTypeBuilder<BoardMember> b)
    {
        b.ToTable("board_members");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.MemberId).HasColumnName("member_id").IsRequired();
        b.Property(e => e.Role).HasColumnName("role").HasMaxLength(100).IsRequired();
        b.Property(e => e.StartDate).HasColumnName("start_date").IsRequired();
        b.Property(e => e.EndDate).HasColumnName("end_date");
        b.Property(e => e.Notes).HasColumnName("notes");
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(e => e.TenantId);
        b.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId);
    }
}

public class ResolutionConfiguration : IEntityTypeConfiguration<Resolution>
{
    public void Configure(EntityTypeBuilder<Resolution> b)
    {
        b.ToTable("resolutions");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        b.Property(e => e.Content).HasColumnName("content").IsRequired();
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        b.Property(e => e.MeetingId).HasColumnName("meeting_id").HasMaxLength(36);
        b.Property(e => e.AdoptedAt).HasColumnName("adopted_at");
        b.Property(e => e.VotesFor).HasColumnName("votes_for").HasDefaultValue(0);
        b.Property(e => e.VotesAgainst).HasColumnName("votes_against").HasDefaultValue(0);
        b.Property(e => e.Abstentions).HasColumnName("abstentions").HasDefaultValue(0);
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(e => e.TenantId);
    }
}
