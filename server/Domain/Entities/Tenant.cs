namespace server.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = "free";
    public string SubscriptionStatus { get; set; } = "trial";
    public string BaseCurrency { get; set; } = "EUR";
    public string CurrencySymbol { get; set; } = "€";
    public string CountryCode { get; set; } = "FR";
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Member> Members { get; set; } = [];
}
