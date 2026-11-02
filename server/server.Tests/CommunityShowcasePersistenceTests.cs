using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;

namespace server.Tests;

public class CommunityShowcasePersistenceTests
{
    [Fact]
    public void Showcase_schema_uses_tenant_scoped_slugs_and_one_showcase_per_member()
    {
        using var db = TestDbFactory.Create();
        var entity = db.Model.FindEntityType(typeof(CommunityShowcase))!;

        Assert.Equal("community_showcases", entity.GetTableName());
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(CommunityShowcase.TenantId), nameof(CommunityShowcase.Slug)]));
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(CommunityShowcase.TenantId), nameof(CommunityShowcase.MemberId)]));
    }

    [Fact]
    public void Offer_schema_is_owned_by_showcase_and_uses_decimal_price()
    {
        using var db = TestDbFactory.Create();
        var offer = db.Model.FindEntityType(typeof(CommunityShowcaseOffer))!;
        var foreignKey = Assert.Single(offer.GetForeignKeys());
        var price = offer.FindProperty(nameof(CommunityShowcaseOffer.Price))!;

        Assert.Equal("community_showcase_offers", offer.GetTableName());
        Assert.Equal(nameof(CommunityShowcaseOffer.ShowcaseId), Assert.Single(foreignKey.Properties).Name);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
        Assert.Equal(12, price.GetPrecision());
        Assert.Equal(2, price.GetScale());
    }

    [Fact]
    public void Report_schema_tracks_moderation_decisions()
    {
        using var db = TestDbFactory.Create();
        var report = db.Model.FindEntityType(typeof(CommunityShowcaseReport))!;

        Assert.Equal("community_showcase_reports", report.GetTableName());
        Assert.NotNull(report.FindProperty(nameof(CommunityShowcaseReport.HandledByUserId)));
        Assert.NotNull(report.FindProperty(nameof(CommunityShowcaseReport.ResolutionNote)));
        Assert.NotNull(report.FindProperty(nameof(CommunityShowcaseReport.HandledAt)));
    }
}
