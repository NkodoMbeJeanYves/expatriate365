using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.MembershipNumber).HasMaxLength(50).IsRequired();
        b.HasIndex(e => new { e.TenantId, e.MembershipNumber }).IsUnique();
        b.Property(e => e.Status).HasMaxLength(20).IsRequired();
        b.Property(e => e.PhotoUrl).HasMaxLength(500);
        b.Property(e => e.Profession).HasMaxLength(200);
        b.Property(e => e.Gender).HasMaxLength(15);
        b.Property(e => e.EmergencyContactName).HasMaxLength(200);
        b.Property(e => e.EmergencyContactPhone).HasMaxLength(20);

        b.HasOne(e => e.Tenant).WithMany(t => t.Members).HasForeignKey(e => e.TenantId);
        b.HasOne(e => e.User).WithMany(u => u.Members).HasForeignKey(e => e.UserId);
        b.HasOne(e => e.Category).WithMany(c => c.Members).HasForeignKey(e => e.CategoryId);
    }
}
