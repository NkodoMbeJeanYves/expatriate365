using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using server.Application.TenantSettings;
using server.Infrastructure.Services;

namespace server.Tests;

public sealed class CurrencyValidationTests
{
    [Theory]
    [InlineData("MU", "MUR", true)]
    [InlineData(" mu ", " mur ", true)]
    [InlineData("RS", "RSD", true)]
    [InlineData("MU", "EUR", false)]
    [InlineData("CM", "XOF", false)]
    [InlineData("CM", "XAF", true)]
    [InlineData("LS", "LSL", true)]
    [InlineData("LS", "ZAR", true)]
    [InlineData("ZW", "ZWL", false)]
    [InlineData("XX", "EUR", false)]
    [InlineData("", "", false)]
    public void CountryCurrencyConsistency_IsIndependentOfProviderCoverage(string country, string currency, bool valid)
        => Assert.Equal(valid, CountryCurrencies.IsValid(country, currency));

    [Fact]
    public async Task GeographicalMismatch_DoesNotCallProvider()
    {
        var catalogue = new StubCatalogue("covered");
        var result = await Service(catalogue).ValidateAsync(new("MU", "EUR"), default);
        Assert.False(result.IsConsistent);
        Assert.Equal(["MUR"], result.AllowedCurrencies);
        Assert.Equal("not_checked", result.CoverageStatus);
        Assert.Equal(0, catalogue.Calls);
    }

    [Theory]
    [InlineData("covered")]
    [InlineData("not_covered")]
    [InlineData("unavailable")]
    public async Task ValidLocalCurrency_RemainsValidRegardlessOfProviderStatus(string status)
    {
        var catalogue = new StubCatalogue(status);
        var result = await Service(catalogue).ValidateAsync(new(" rs ", " rsd "), default);
        Assert.True(result.IsConsistent);
        Assert.Equal("RS", result.CountryCode);
        Assert.Equal("RSD", result.BaseCurrency);
        Assert.Equal(status, result.CoverageStatus);
        Assert.Equal("Frankfurter", result.Source);
        Assert.Equal(1, catalogue.Calls);
    }

    [Fact]
    public async Task Catalogue_CachesAndCoalescesFor24Hours_AndRefreshesAfterExpiry()
    {
        var clock = new TestClock();
        using var handler = new CatalogueHandler("""[{"iso_code":"MUR"},{"iso_code":"RSD"}]""");
        using var catalogue = Catalogue(handler, clock);
        var requests = Enumerable.Range(0, 10).Select(_ => catalogue.CheckAsync("MUR", default));
        Assert.All(await Task.WhenAll(requests), result => Assert.Equal("covered", result.Status));
        Assert.Equal(1, handler.Calls);
        Assert.Equal("not_covered", (await catalogue.CheckAsync("ZZZ", default)).Status);
        clock.Advance(TimeSpan.FromHours(23));
        var cached = await catalogue.CheckAsync("RSD", default);
        Assert.Equal(clock.GetUtcNow().AddHours(-23), cached.CheckedAt);
        Assert.Equal(1, handler.Calls);
        clock.Advance(TimeSpan.FromHours(1));
        var refreshed = await catalogue.CheckAsync("RSD", default);
        Assert.Equal(clock.GetUtcNow(), refreshed.CheckedAt);
        Assert.Equal(2, handler.Calls);
        Assert.All(handler.Requests, uri => Assert.Equal("https://api.frankfurter.dev/v2/currencies", uri));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("not-json")]
    [InlineData("""[{"iso_code":"MUR"},{"iso_code":"MUR"}]""")]
    [InlineData("""[{"iso_code":"mur"}]""")]
    [InlineData("""[{"name":"MUR"}]""")]
    public async Task InvalidCatalogue_IsUnavailable_NotAnUnsupportedCurrency(string json)
    {
        using var handler = new CatalogueHandler(json);
        using var catalogue = Catalogue(handler, new TestClock());
        var result = await catalogue.CheckAsync("MUR", default);
        Assert.Equal("unavailable", result.Status);
        Assert.Null(result.CheckedAt);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ProviderFailure_IsExplicitAndDoesNotReuseExpiredCoverage(HttpStatusCode failure)
    {
        var clock = new TestClock();
        using var handler = new CatalogueHandler("""[{"iso_code":"MUR"}]""");
        using var catalogue = Catalogue(handler, clock);
        Assert.Equal("covered", (await catalogue.CheckAsync("MUR", default)).Status);
        clock.Advance(TimeSpan.FromHours(24));
        handler.Status = failure;
        Assert.Equal("unavailable", (await catalogue.CheckAsync("MUR", default)).Status);
        Assert.Equal("unavailable", (await catalogue.CheckAsync("MUR", default)).Status);
        Assert.Equal(2, handler.Calls);
        clock.Advance(TimeSpan.FromMinutes(1));
        handler.Status = HttpStatusCode.OK;
        Assert.Equal("covered", (await catalogue.CheckAsync("MUR", default)).Status);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task CallerCancellation_IsNotReportedAsProviderFailure()
    {
        using var handler = new CatalogueHandler("[]");
        using var catalogue = Catalogue(handler, new TestClock());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => catalogue.CheckAsync("MUR", cts.Token));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TimeoutOrNetworkFailure_IsReportedAsUnavailable(bool timeout)
    {
        using var handler = new CatalogueHandler("[]")
        {
            Failure = timeout ? new TaskCanceledException("Timeout") : new HttpRequestException("Network failure")
        };
        using var catalogue = Catalogue(handler, new TestClock());
        var result = await catalogue.CheckAsync("MUR", default);
        Assert.Equal("unavailable", result.Status);
        Assert.Null(result.CheckedAt);
    }

    private static CurrencyValidationService Service(ICurrencyCatalogue catalogue)
        => new(catalogue, NullLogger<CurrencyValidationService>.Instance);

    private static FrankfurterCurrencyCatalogue Catalogue(CatalogueHandler handler, TimeProvider clock)
        => new(new TestClientFactory(handler), clock, NullLogger<FrankfurterCurrencyCatalogue>.Instance);

    private sealed class StubCatalogue(string status) : ICurrencyCatalogue
    {
        internal int Calls;
        public Task<CurrencyCoverage> CheckAsync(string currency, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new CurrencyCoverage(status, null));
        }
    }

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 10, 22, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        internal void Advance(TimeSpan duration) => now += duration;
    }

    private sealed class CatalogueHandler(string json) : HttpMessageHandler
    {
        internal int Calls;
        internal HttpStatusCode Status = HttpStatusCode.OK;
        internal Exception? Failure;
        internal List<string> Requests = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            Requests.Add(request.RequestUri!.AbsoluteUri);
            if (Failure is not null)
                return Task.FromException<HttpResponseMessage>(Failure);
            return Task.FromResult(new HttpResponseMessage(Status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class TestClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.frankfurter.dev/") };
    }
}
