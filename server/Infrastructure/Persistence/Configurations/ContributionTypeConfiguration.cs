using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class ContributionTypeConfiguration : IEntityTypeConfiguration<ContributionType>
{
    public void Configure(EntityTypeBuilder<ContributionType> b)
    {
        b.ToTable("contribution_types");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        b.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
        b.Property(e => e.Frequency).HasColumnName("frequency").HasMaxLength(50).IsRequired();
        b.Property(e => e.BaseAmount).HasColumnName("base_amount").HasColumnType("decimal(18,2)");
        b.Property(e => e.LatePenaltyRate).HasColumnName("late_penalty_rate").HasColumnType("decimal(5,4)");
        b.Property(e => e.GracePeriodDays).HasColumnName("grace_period_days");
        b.Property(e => e.IsActive).HasColumnName("is_active");
        b.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
        b.Property(e => e.EffectiveTo).HasColumnName("effective_to");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
    }
}
