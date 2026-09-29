namespace server.Infrastructure.Services;

public class EmailSettings
{
    public string From { get; set; } = "noreply@expatriate365.mu";
    public string FromName { get; set; } = "Expatriate365";
    public SmtpSettings Smtp { get; set; } = new();
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
