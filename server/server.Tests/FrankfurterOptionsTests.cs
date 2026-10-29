using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using server.Infrastructure.Services;

namespace server.Tests;

public sealed class FrankfurterOptionsTests
{
    [Fact]
    public void Defaults_target_public_api_with_five_second_timeout_and_system_proxy()
    {
        var options = new FrankfurterOptions();

        Assert.True(new FrankfurterOptionsValidator().Validate(null, options).Succeeded);
        Assert.Equal(new Uri("https://api.frankfurter.dev/"), options.GetBaseAddress());
        Assert.Equal(5, options.TimeoutSeconds);
        using var handler = Assert.IsType<SocketsHttpHandler>(options.CreateHandler());
        Assert.Null(handler.Proxy);
    }

    [Theory]
    [InlineData("https://fx.internal.example", "https://fx.internal.example/")]
    [InlineData(" http://frankfurter:8080/api ", "http://frankfurter:8080/api/")]
    [InlineData("https://fx.example/api/", "https://fx.example/api/")]
    public void Self_hosted_base_url_gets_trailing_slash_so_relative_paths_resolve(string configured, string expected)
    {
        var options = new FrankfurterOptions { BaseUrl = configured };

        Assert.True(new FrankfurterOptionsValidator().Validate(null, options).Succeeded);
        Assert.Equal(new Uri(expected), options.GetBaseAddress());
        Assert.Equal(new Uri(expected + "v2/currencies"), new Uri(options.GetBaseAddress(), "v2/currencies"));
    }

    [Theory]
    [InlineData("", 5, null)]
    [InlineData("api.frankfurter.dev", 5, null)]
    [InlineData("ftp://api.frankfurter.dev", 5, null)]
    [InlineData("https://api.frankfurter.dev/?x=1", 5, null)]
    [InlineData("https://api.frankfurter.dev", 0, null)]
    [InlineData("https://api.frankfurter.dev", 61, null)]
    [InlineData("https://api.frankfurter.dev", 5, "proxy:3128")]
    [InlineData("https://api.frankfurter.dev", 5, "socks://proxy:1080")]
    public void Invalid_settings_are_rejected(string baseUrl, int timeout, string? proxy)
    {
        var options = new FrankfurterOptions { BaseUrl = baseUrl, TimeoutSeconds = timeout, ProxyUrl = proxy };

        Assert.True(new FrankfurterOptionsValidator().Validate(null, options).Failed);
    }

    [Fact]
    public void Explicit_proxy_is_applied_to_handler()
    {
        var options = new FrankfurterOptions
        {
            ProxyUrl = "http://proxy.corp:3128",
            ProxyUseDefaultCredentials = true,
            ProxyBypassOnLocal = false
        };

        Assert.True(new FrankfurterOptionsValidator().Validate(null, options).Succeeded);
        using var handler = Assert.IsType<SocketsHttpHandler>(options.CreateHandler());
        var proxy = Assert.IsType<WebProxy>(handler.Proxy);
        Assert.True(handler.UseProxy);
        Assert.Equal(new Uri("http://proxy.corp:3128/"), proxy.Address);
        Assert.True(proxy.UseDefaultCredentials);
        Assert.False(proxy.BypassProxyOnLocal);
    }

    [Fact]
    public void Configuration_section_drives_named_client()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Frankfurter:BaseUrl"] = "https://fx.internal.example/frankfurter",
            ["Frankfurter:TimeoutSeconds"] = "12"
        }).Build();
        var services = new ServiceCollection();
        services.AddFrankfurterHttpClient(config);
        using var provider = services.BuildServiceProvider();

        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(FrankfurterServiceCollectionExtensions.ClientName);

        Assert.Equal(new Uri("https://fx.internal.example/frankfurter/"), client.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(12), client.Timeout);
    }

    [Fact]
    public void Invalid_configuration_fails_when_options_are_resolved()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Frankfurter:TimeoutSeconds"] = "0"
        }).Build();
        var services = new ServiceCollection();
        services.AddFrankfurterHttpClient(config);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<FrankfurterOptions>>().Value);
    }
}
