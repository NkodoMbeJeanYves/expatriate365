using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class BoardRoleConfiguration : IEntityTypeConfiguration<BoardRole>
{
    public void Configure(EntityTypeBuilder<BoardRole> b)
    {
        b.ToTable("board_roles");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(e => e.Label).HasColumnName("label").HasMaxLength(200).IsRequired();
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
        b.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(e => e.BoardMembers).WithOne(bm => bm.BoardRole).HasForeignKey(bm => bm.RoleId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class BoardMemberConfiguration : IEntityTypeConfiguration<BoardMember>
{
    public void Configure(EntityTypeBuilder<BoardMember> b)
    {
        b.ToTable("board_members");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.MemberId).HasColumnName("member_id").IsRequired();
        b.Property(e => e.RoleId).HasColumnName("role_id");
        b.Property(e => e.StartDate).HasColumnName("start_date").IsRequired();
        b.Property(e => e.EndDate).HasColumnName("end_date");
        b.Property(e => e.Notes).HasColumnName("notes");
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(e => e.TenantId);
        b.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId).OnDelete(DeleteBehavior.Cascade);
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
