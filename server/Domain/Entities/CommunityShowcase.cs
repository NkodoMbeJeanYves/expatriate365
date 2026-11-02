namespace server.Domain.Entities;

public class CommunityShowcase
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MemberId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ServiceArea { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactLink { get; set; }
    public bool ContactEnabled { get; set; }
    public string? PhotoUrl { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Member Member { get; set; } = null!;
    public ICollection<CommunityShowcaseOffer> Offers { get; set; } = [];
}

public class CommunityShowcaseOffer
{
    public Guid Id { get; set; }
    public Guid ShowcaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public bool PriceOnRequest { get; set; }
    public string? Details { get; set; }
    public int Position { get; set; }

    public CommunityShowcase Showcase { get; set; } = null!;
}

public class CommunityShowcaseReport
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ShowcaseId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public Guid? HandledByUserId { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? HandledAt { get; set; }

    public CommunityShowcase Showcase { get; set; } = null!;
}
