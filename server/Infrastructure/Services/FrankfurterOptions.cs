using System.Net;
using Microsoft.Extensions.Options;

namespace server.Infrastructure.Services;

/// <summary>Frankfurter exchange-rate provider settings (public API or self-hosted instance).</summary>
public sealed class FrankfurterOptions
{
    public const string SectionName = "Frankfurter";
    public const int MinTimeoutSeconds = 1;
    public const int MaxTimeoutSeconds = 60;

    public string BaseUrl { get; set; } = "https://api.frankfurter.dev/";
    public int TimeoutSeconds { get; set; } = 5;
    /// <summary>Optional explicit proxy; when empty, the system proxy settings apply.</summary>
    public string? ProxyUrl { get; set; }
    public bool ProxyUseDefaultCredentials { get; set; }
    public bool ProxyBypassOnLocal { get; set; } = true;

    /// <summary>Base address with a trailing slash so relative paths such as "v2/currencies" resolve correctly.</summary>
    public Uri GetBaseAddress()
    {
        var url = BaseUrl.Trim();
        return new Uri(url.EndsWith('/') ? url : url + "/", UriKind.Absolute);
    }

    public HttpMessageHandler CreateHandler()
    {
        var handler = new SocketsHttpHandler();
        if (!string.IsNullOrWhiteSpace(ProxyUrl))
        {
            handler.Proxy = new WebProxy(new Uri(ProxyUrl.Trim(), UriKind.Absolute), ProxyBypassOnLocal)
            {
                UseDefaultCredentials = ProxyUseDefaultCredentials
            };
            handler.UseProxy = true;
        }
        return handler;
    }

    internal static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
        string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
}

public static class FrankfurterServiceCollectionExtensions
{
    public const string ClientName = "Frankfurter";

    public static IServiceCollection AddFrankfurterHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FrankfurterOptions>()
            .Bind(configuration.GetSection(FrankfurterOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<FrankfurterOptions>, FrankfurterOptionsValidator>();
        services.AddHttpClient(ClientName, (sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<FrankfurterOptions>>().Value;
                client.BaseAddress = options.GetBaseAddress();
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
                sp.GetRequiredService<IOptions<FrankfurterOptions>>().Value.CreateHandler());
        return services;
    }
}

public sealed class FrankfurterOptionsValidator : IValidateOptions<FrankfurterOptions>
{
    public ValidateOptionsResult Validate(string? name, FrankfurterOptions options)
    {
        var errors = new List<string>();
        if (!FrankfurterOptions.IsHttpUrl(options.BaseUrl))
            errors.Add("Frankfurter:BaseUrl must be an absolute http(s) URL without query or fragment.");
        if (options.TimeoutSeconds is < FrankfurterOptions.MinTimeoutSeconds or > FrankfurterOptions.MaxTimeoutSeconds)
            errors.Add($"Frankfurter:TimeoutSeconds must be between {FrankfurterOptions.MinTimeoutSeconds} and {FrankfurterOptions.MaxTimeoutSeconds}.");
        if (!string.IsNullOrWhiteSpace(options.ProxyUrl) && !FrankfurterOptions.IsHttpUrl(options.ProxyUrl))
            errors.Add("Frankfurter:ProxyUrl must be empty or an absolute http(s) URL.");
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
