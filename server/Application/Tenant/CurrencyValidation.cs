namespace server.Application.TenantSettings;

/// <summary>Country and ISO currency selected for association configuration.</summary>
public sealed record CurrencyValidationRequest(string CountryCode, string BaseCurrency);

/// <summary>Local consistency and independent, time-stamped Frankfurter catalogue coverage.</summary>
public sealed record CurrencyValidationResponse(
    string CountryCode, string BaseCurrency, bool IsConsistent, IReadOnlyList<string> AllowedCurrencies,
    string CoverageStatus, string Source, DateTimeOffset? CheckedAt);

public sealed record CurrencyCoverage(string Status, DateTimeOffset? CheckedAt);

public interface ICurrencyCatalogue
{
    Task<CurrencyCoverage> CheckAsync(string currency, CancellationToken ct);
}

public sealed class CurrencyValidationService(
    ICurrencyCatalogue catalogue, ILogger<CurrencyValidationService> log)
{
    public async Task<CurrencyValidationResponse> ValidateAsync(CurrencyValidationRequest request, CancellationToken ct)
    {
        var country = CountryCurrencies.Normalize(request.CountryCode);
        var currency = CountryCurrencies.Normalize(request.BaseCurrency);
        var valid = CountryCurrencies.IsValid(country, currency);
        log.LogInformation("Validating association monetary configuration {Country} {Currency}: {IsConsistent}",
            country, currency, valid);
        var coverage = valid ? await catalogue.CheckAsync(currency, ct) : new CurrencyCoverage("not_checked", null);
        return new(country, currency, valid, CountryCurrencies.Get(country), coverage.Status, "Frankfurter", coverage.CheckedAt);
    }
}
