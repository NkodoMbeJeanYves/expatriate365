using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace server.Application.Members.Services;

public static class MemberEmailGenerator
{
    public static async Task<string> GenerateAsync(
        string firstName, string lastName,
        Guid tenantId, AppDbContext db, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        var slug        = tenant?.Slug ?? "org";
        var countryCode = (tenant?.CountryCode ?? "MU").ToLowerInvariant();
        var domain      = $"{slug}.{countryCode}";

        var first = Normalize(firstName);
        var last  = Normalize(lastName);
        var baseLocal = $"{first}.{last}";

        // Load all taken addresses for this base in one query to avoid N+1.
        var prefix = $"{baseLocal}@{domain}";
        var taken = await db.Users
            .Where(u => u.Email == prefix || u.Email.StartsWith($"{baseLocal}."))
            .Select(u => u.Email)
            .ToListAsync(ct);

        if (!taken.Contains(prefix))
            return prefix;

        var takenSet = taken.ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (int i = 2; i <= 9999; i++)
        {
            var candidate = $"{baseLocal}.{i:D5}@{domain}";
            if (!takenSet.Contains(candidate))
                return candidate;
        }

        // Fallback with full guid suffix (should never happen in practice)
        return $"{baseLocal}.{Guid.NewGuid():N}@{domain}";
    }

    private static string Normalize(string input)
    {
        // Remove accents
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);

        var clean = sb.ToString().Normalize(NormalizationForm.FormC);
        // lowercase, replace spaces/hyphens with dot, keep only alphanum and dot
        clean = clean.ToLowerInvariant();
        clean = Regex.Replace(clean, @"[\s\-_]+", ".");
        clean = Regex.Replace(clean, @"[^a-z0-9.]", "");
        clean = Regex.Replace(clean, @"\.{2,}", ".");
        return clean.Trim('.');
    }
}
