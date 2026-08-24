using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).HasMaxLength(200).IsRequired();
        b.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        b.HasIndex(e => e.Slug).IsUnique();
        b.Property(e => e.BaseCurrency).HasMaxLength(3).IsRequired();
        b.Property(e => e.CountryCode).HasMaxLength(2).IsRequired();
        b.Property(e => e.LogoUrl).HasMaxLength(500);
        b.Property(e => e.SubscriptionTier).HasMaxLength(20).IsRequired();
        b.Property(e => e.SubscriptionStatus).HasMaxLength(20).IsRequired();
    }
}
