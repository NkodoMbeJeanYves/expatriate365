using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using server.Application.TenantSettings;

namespace server.Infrastructure.Services;

public sealed class FrankfurterCurrencyCatalogue(
    IHttpClientFactory clients, TimeProvider clock, ILogger<FrankfurterCurrencyCatalogue> log)
    : ICurrencyCatalogue, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private HashSet<string>? codes;
    private DateTimeOffset? checkedAt;
    private DateTimeOffset retryAt;

    public async Task<CurrencyCoverage> CheckAsync(string currency, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var now = clock.GetUtcNow();
            if (checkedAt is null || now - checkedAt >= TimeSpan.FromHours(24))
            {
                if (now < retryAt)
                    return new("unavailable", null);
                try
                {
                    using var client = clients.CreateClient(FrankfurterServiceCollectionExtensions.ClientName);
                    log.LogInformation("Refreshing Frankfurter current currency catalogue from {BaseAddress}", client.BaseAddress);
                    var entries = await client.GetFromJsonAsync<CurrencyEntry?[]>("v2/currencies", ct);
                    if (entries is null || entries.Length == 0 ||
                        entries.Any(e => e?.IsoCode is null || e.IsoCode.Length != 3 ||
                            e.IsoCode.Any(c => c is < 'A' or > 'Z')) ||
                        entries.Select(e => e!.IsoCode).Distinct(StringComparer.Ordinal).Count() != entries.Length)
                        throw new JsonException("Invalid Frankfurter currency catalogue.");
                    codes = entries.Select(e => e!.IsoCode!).ToHashSet(StringComparer.Ordinal);
                    checkedAt = clock.GetUtcNow();
                }
                catch (Exception ex) when (ex is HttpRequestException or JsonException or IOException ||
                    ex is OperationCanceledException && !ct.IsCancellationRequested)
                {
                    log.LogWarning(ex, "Frankfurter currency catalogue unavailable; monetary configuration remains locally validated");
                    retryAt = clock.GetUtcNow().AddMinutes(1);
                    return new("unavailable", null);
                }
            }
            return new(codes!.Contains(currency) ? "covered" : "not_covered", checkedAt);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose() => gate.Dispose();

    private sealed record CurrencyEntry([property: JsonPropertyName("iso_code")] string? IsoCode);
}
