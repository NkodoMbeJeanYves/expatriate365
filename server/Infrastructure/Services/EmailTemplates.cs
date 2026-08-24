namespace server.Infrastructure.Services;

public static class EmailTemplates
{
    private const string CardStyle  = "background:#fff;border-radius:12px;max-width:560px;margin:0 auto;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08)";
    private const string HeaderStyle = "background:#059669;padding:24px 32px";
    private const string H1Style    = "color:#fff;margin:0;font-size:1.1rem;font-weight:600";
    private const string BodyStyle  = "padding:28px 32px;color:#374151;font-size:.95rem;line-height:1.6";
    private const string FooterStyle = "padding:16px 32px;background:#f8fafc;color:#94a3b8;font-size:.8rem;text-align:center";
    private const string AmountStyle = "font-size:1.5rem;font-weight:700;color:#059669";
    private const string BadgeWarn  = "display:inline-block;padding:4px 12px;border-radius:20px;font-size:.8rem;font-weight:600;background:#fef3c7;color:#b45309";
    private const string BadgeOk    = "display:inline-block;padding:4px 12px;border-radius:20px;font-size:.8rem;font-weight:600;background:#d1fae5;color:#065f46";
    private const string TdLabel    = "padding:8px 0;color:#6b7280;width:40%;vertical-align:top";
    private const string TdValue    = "padding:8px 0;vertical-align:top";

    private static string Wrap(string title, string body) =>
        $"""
        <!DOCTYPE html><html lang="fr"><head><meta charset="UTF-8"/></head>
        <body style="font-family:Segoe UI,Arial,sans-serif;background:#f1f5f9;margin:0;padding:24px">
          <div style="{CardStyle}">
            <div style="{HeaderStyle}"><h1 style="{H1Style}">Expatriate365</h1></div>
            <div style="{BodyStyle}">
              <h2 style="margin-top:0;font-size:1.1rem;color:#111827">{title}</h2>
              {body}
            </div>
            <div style="{FooterStyle}">Expatriate365 &middot; Ne pas r&eacute;pondre &agrave; cet email</div>
          </div>
        </body></html>
        """;

    public static string ChargeGenerated(string memberName, string typeName, decimal amount, string dueDate) =>
        Wrap("Nouvelle &eacute;ch&eacute;ance de cotisation",
            $"""
            <p>Bonjour <strong>{memberName}</strong>,</p>
            <p>Une nouvelle &eacute;ch&eacute;ance a &eacute;t&eacute; g&eacute;n&eacute;r&eacute;e :</p>
            <table style="width:100%;border-collapse:collapse;margin:16px 0">
              <tr><td style="{TdLabel}">Type</td><td style="{TdValue};font-weight:600">{typeName}</td></tr>
              <tr><td style="{TdLabel}">Montant d&ucirc;</td><td style="{TdValue}"><span style="{AmountStyle}">{amount:N0} FCFA</span></td></tr>
              <tr><td style="{TdLabel}">&Eacute;ch&eacute;ance</td><td style="{TdValue}"><span style="{BadgeWarn}">{dueDate}</span></td></tr>
            </table>
            <p>Connectez-vous &agrave; votre espace pour effectuer votre r&egrave;glement.</p>
            """);

    public static string PaymentConfirmed(string memberName, string typeName, decimal amount, string receiptNumber) =>
        Wrap("Paiement confirm&eacute;",
            $"""
            <p>Bonjour <strong>{memberName}</strong>,</p>
            <p>Votre paiement a &eacute;t&eacute; <strong>confirm&eacute;</strong> par le bureau :</p>
            <table style="width:100%;border-collapse:collapse;margin:16px 0">
              <tr><td style="{TdLabel}">Cotisation</td><td style="{TdValue};font-weight:600">{typeName}</td></tr>
              <tr><td style="{TdLabel}">Montant</td><td style="{TdValue}"><span style="{AmountStyle}">{amount:N0} FCFA</span></td></tr>
              <tr><td style="{TdLabel}">Re&ccedil;u N&deg;</td><td style="{TdValue}"><span style="{BadgeOk}">{receiptNumber}</span></td></tr>
            </table>
            <p>Merci pour votre r&egrave;glement.</p>
            """);
}
