namespace server.Infrastructure.Services;

public static class EmailAddressMasking
{
    public static string Mask(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "(empty)";
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        return $"{email[0]}***{email[at..]}";
    }
}
