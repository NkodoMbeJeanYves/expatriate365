namespace server.Application.TenantSettings;

public static class CountryCurrencies
{
    private static readonly IReadOnlyDictionary<string, string[]> LocalCurrencies =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["DZ"] = ["DZD"], ["AO"] = ["AOA"], ["AU"] = ["AUD"], ["AT"] = ["EUR"],
            ["BE"] = ["EUR"], ["BJ"] = ["XOF"], ["BR"] = ["BRL"], ["BF"] = ["XOF"],
            ["BI"] = ["BIF"], ["CM"] = ["XAF"], ["CA"] = ["CAD"], ["CV"] = ["CVE"],
            ["CF"] = ["XAF"], ["TD"] = ["XAF"], ["CN"] = ["CNY"], ["KM"] = ["KMF"],
            ["CG"] = ["XAF"], ["CD"] = ["CDF"], ["CI"] = ["XOF"], ["DJ"] = ["DJF"],
            ["EG"] = ["EGP"], ["GQ"] = ["XAF"], ["ER"] = ["ERN"], ["ET"] = ["ETB"],
            ["FR"] = ["EUR"], ["GA"] = ["XAF"], ["GM"] = ["GMD"], ["DE"] = ["EUR"],
            ["GH"] = ["GHS"], ["GN"] = ["GNF"], ["GW"] = ["XOF"], ["IN"] = ["INR"],
            ["IT"] = ["EUR"], ["KE"] = ["KES"], ["LS"] = ["LSL", "ZAR"], ["LR"] = ["LRD", "USD"],
            ["LY"] = ["LYD"], ["MG"] = ["MGA"], ["MW"] = ["MWK"], ["ML"] = ["XOF"],
            ["MR"] = ["MRU"], ["MU"] = ["MUR"], ["MA"] = ["MAD"], ["MZ"] = ["MZN"],
            ["NA"] = ["NAD", "ZAR"], ["NL"] = ["EUR"], ["NE"] = ["XOF"], ["NG"] = ["NGN"],
            ["PT"] = ["EUR"], ["RE"] = ["EUR"], ["RW"] = ["RWF"], ["ST"] = ["STN"],
            ["SN"] = ["XOF"], ["RS"] = ["RSD"], ["SC"] = ["SCR"], ["SL"] = ["SLE"],
            ["SO"] = ["SOS"], ["ZA"] = ["ZAR"], ["SS"] = ["SSP"], ["ES"] = ["EUR"],
            ["SD"] = ["SDG"], ["SZ"] = ["SZL", "ZAR"], ["CH"] = ["CHF"], ["TZ"] = ["TZS"],
            ["TG"] = ["XOF"], ["TN"] = ["TND"], ["UG"] = ["UGX"], ["AE"] = ["AED"],
            ["GB"] = ["GBP"], ["US"] = ["USD"], ["ZM"] = ["ZMW"], ["ZW"] = ["ZWG", "USD"]
        };

    public static string Normalize(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    public static IReadOnlyList<string> Get(string? country)
        => LocalCurrencies.TryGetValue(Normalize(country), out var currencies) ? currencies : [];

    public static bool IsValid(string? country, string? currency)
        => Get(country).Contains(Normalize(currency), StringComparer.Ordinal);
}
