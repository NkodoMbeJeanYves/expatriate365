using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class CommunityShowcaseConfiguration : IEntityTypeConfiguration<CommunityShowcase>
{
    public void Configure(EntityTypeBuilder<CommunityShowcase> b)
    {
        b.ToTable("community_showcases");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.MemberId).HasColumnName("member_id").IsRequired();
        b.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(120).IsRequired();
        b.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(160).IsRequired();
        b.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        b.Property(e => e.Description).HasColumnName("description").HasMaxLength(4000).IsRequired();
        b.Property(e => e.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
        b.Property(e => e.ServiceArea).HasColumnName("service_area").HasMaxLength(200);
        b.Property(e => e.ContactPhone).HasColumnName("contact_phone").HasMaxLength(40);
        b.Property(e => e.ContactLink).HasColumnName("contact_link").HasMaxLength(500);
        b.Property(e => e.PhotoUrl).HasColumnName("photo_url").HasMaxLength(1000);
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("draft").IsRequired();
        b.Property(e => e.PublishedAt).HasColumnName("published_at");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();
        b.HasIndex(e => new { e.TenantId, e.MemberId }).IsUnique();
        b.HasIndex(e => new { e.TenantId, e.Status });
        b.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(e => e.Offers).WithOne(o => o.Showcase).HasForeignKey(o => o.ShowcaseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CommunityShowcaseOfferConfiguration : IEntityTypeConfiguration<CommunityShowcaseOffer>
{
    public void Configure(EntityTypeBuilder<CommunityShowcaseOffer> b)
    {
        b.ToTable("community_showcase_offers");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.ShowcaseId).HasColumnName("showcase_id").IsRequired();
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        b.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();
        b.Property(e => e.Price).HasColumnName("price").HasPrecision(12, 2);
        b.Property(e => e.PriceOnRequest).HasColumnName("price_on_request");
        b.Property(e => e.Details).HasColumnName("details").HasMaxLength(1000);
        b.Property(e => e.Position).HasColumnName("position");
        b.HasIndex(e => new { e.ShowcaseId, e.Position });
    }
}

public class CommunityShowcaseReportConfiguration : IEntityTypeConfiguration<CommunityShowcaseReport>
{
    public void Configure(EntityTypeBuilder<CommunityShowcaseReport> b)
    {
        b.ToTable("community_showcase_reports");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.ShowcaseId).HasColumnName("showcase_id").IsRequired();
        b.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("pending").IsRequired();
        b.Property(e => e.HandledByUserId).HasColumnName("handled_by_user_id");
        b.Property(e => e.ResolutionNote).HasColumnName("resolution_note").HasMaxLength(1000);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.HandledAt).HasColumnName("handled_at");
        b.HasIndex(e => new { e.TenantId, e.Status, e.CreatedAt });
        b.HasOne(e => e.Showcase).WithMany().HasForeignKey(e => e.ShowcaseId).OnDelete(DeleteBehavior.Restrict);
    }
}
