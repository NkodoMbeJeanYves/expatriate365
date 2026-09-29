namespace server.Infrastructure.Services;

public static class EmailTemplates
{
    private const string CardStyle   = "background:#fff;border-radius:12px;max-width:560px;margin:0 auto;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08)";
    private const string HeaderStyle = "background:#059669;padding:24px 32px";
    private const string H1Style    = "color:#fff;margin:0;font-size:1.1rem;font-weight:600";
    private const string BodyStyle  = "padding:28px 32px;color:#374151;font-size:.95rem;line-height:1.6";
    private const string FooterStyle = "padding:16px 32px;background:#f8fafc;color:#94a3b8;font-size:.8rem;text-align:center";
    private const string AmountStyle = "font-size:1.5rem;font-weight:700;color:#059669";
    private const string BadgeWarn  = "display:inline-block;padding:4px 12px;border-radius:20px;font-size:.8rem;font-weight:600;background:#fef3c7;color:#b45309";
    private const string BadgeOk    = "display:inline-block;padding:4px 12px;border-radius:20px;font-size:.8rem;font-weight:600;background:#d1fae5;color:#065f46";
    private const string BadgeInfo  = "display:inline-block;padding:4px 12px;border-radius:20px;font-size:.8rem;font-weight:600;background:#dbeafe;color:#1e40af";
    private const string TdLabel    = "padding:8px 0;color:#6b7280;width:40%;vertical-align:top";
    private const string TdValue    = "padding:8px 0;vertical-align:top";
    private const string BtnStyle   = "display:inline-block;margin-top:16px;padding:12px 28px;background:#059669;color:#fff;border-radius:8px;text-decoration:none;font-weight:600;font-size:.95rem";

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

    // ── Identifiants de connexion ────────────────────────────────────────────

    public static string WelcomeOrgAdmin(string fullName, string associationName, string email, string password, string loginUrl) =>
        Wrap($"Bienvenue sur Expatriate365 &mdash; {associationName}",
            $"""
            <p>Bonjour <strong>{fullName}</strong>,</p>
            <p>Votre association <strong>{associationName}</strong> a &eacute;t&eacute; cr&eacute;&eacute;e sur Expatriate365.
            Voici vos identifiants d&rsquo;administrateur :</p>
            <table style="width:100%;border-collapse:collapse;margin:16px 0">
              <tr><td style="{TdLabel}">Email de connexion</td><td style="{TdValue}"><strong>{email}</strong></td></tr>
              <tr><td style="{TdLabel}">Mot de passe</td><td style="{TdValue}"><strong>{password}</strong></td></tr>
            </table>
            <p style="color:#b45309;font-size:.85rem">&#9888; Changez votre mot de passe d&egrave;s votre premi&egrave;re connexion.</p>
            <a href="{loginUrl}" style="{BtnStyle}">Se connecter</a>
            """);

    public static string WelcomeMember(string fullName, string associationName, string email, string password, string loginUrl) =>
        Wrap($"Bienvenue dans {associationName}",
            $"""
            <p>Bonjour <strong>{fullName}</strong>,</p>
            <p>Votre compte membre a &eacute;t&eacute; cr&eacute;&eacute; dans l&rsquo;association <strong>{associationName}</strong>.</p>
            <table style="width:100%;border-collapse:collapse;margin:16px 0">
              <tr><td style="{TdLabel}">Email de connexion</td><td style="{TdValue}"><strong>{email}</strong></td></tr>
              <tr><td style="{TdLabel}">Mot de passe</td><td style="{TdValue}"><strong>{password}</strong></td></tr>
            </table>
            <p style="color:#b45309;font-size:.85rem">&#9888; Changez votre mot de passe d&egrave;s votre premi&egrave;re connexion.</p>
            <a href="{loginUrl}" style="{BtnStyle}">Acc&eacute;der &agrave; mon espace</a>
            """);

    public static string MemberInvitation(string fullName, string associationName, string setPasswordUrl) =>
        Wrap($"Invitation &mdash; {associationName}",
            $"""
            <p>Bonjour <strong>{fullName}</strong>,</p>
            <p>Vous avez &eacute;t&eacute; invit&eacute;(e) &agrave; rejoindre l&rsquo;association <strong>{associationName}</strong> sur Expatriate365.</p>
            <p>Cliquez sur le bouton ci-dessous pour d&eacute;finir votre mot de passe et activer votre compte :</p>
            <a href="{setPasswordUrl}" style="{BtnStyle}">Activer mon compte</a>
            <p style="color:#94a3b8;font-size:.82rem;margin-top:16px">Ce lien expire dans 24h.</p>
            """);

    public static string PasswordReset(string fullName, string resetUrl) =>
        Wrap("R&eacute;initialisation de mot de passe",
            $"""
            <p>Bonjour <strong>{fullName}</strong>,</p>
            <p>Vous avez demand&eacute; la r&eacute;initialisation de votre mot de passe.</p>
            <a href="{resetUrl}" style="{BtnStyle}">R&eacute;initialiser mon mot de passe</a>
            <p style="color:#94a3b8;font-size:.82rem;margin-top:16px">Ce lien expire dans 1h. Si vous n&rsquo;avez pas fait cette demande, ignorez cet email.</p>
            """);

    // ── Événements & réunions ────────────────────────────────────────────────

    public static string EventInvite(string memberName, string eventTitle, string eventDate, string eventLocation, string associationName, string eventUrl) =>
        Wrap($"&Eacute;v&eacute;nement &mdash; {eventTitle}",
            $"""
            <p>Bonjour <strong>{memberName}</strong>,</p>
            <p>L&rsquo;association <strong>{associationName}</strong> organise un &eacute;v&eacute;nement auquel vous &ecirc;tes convit&eacute;(e) :</p>
            <table style="width:100%;border-collapse:collapse;margin:16px 0">
              <tr><td style="{TdLabel}">&Eacute;v&eacute;nement</td><td style="{TdValue}"><strong>{eventTitle}</strong></td></tr>
              <tr><td style="{TdLabel}">Date</td><td style="{TdValue}"><span style="{BadgeInfo}">{eventDate}</span></td></tr>
              <tr><td style="{TdLabel}">Lieu</td><td style="{TdValue}">{eventLocation}</td></tr>
            </table>
            <a href="{eventUrl}" style="{BtnStyle}">Voir l&rsquo;&eacute;v&eacute;nement</a>
            """);

    public static string MeetingConvocation(string memberName, string meetingTitle, string meetingDate, string meetingLocation, string associationName, string agendaUrl) =>
        Wrap($"Convocation &mdash; {meetingTitle}",
            $"""
            <p>Bonjour <strong>{memberName}</strong>,</p>
            <p>Vous &ecirc;tes convoqué(e) &agrave; la r&eacute;union suivante de l&rsquo;association <strong>{associationName}</strong> :</p>
            <table style="width:100%;border-collapse:collapse;margin:16px 0">
              <tr><td style="{TdLabel}">R&eacute;union</td><td style="{TdValue}"><strong>{meetingTitle}</strong></td></tr>
              <tr><td style="{TdLabel}">Date</td><td style="{TdValue}"><span style="{BadgeInfo}">{meetingDate}</span></td></tr>
              <tr><td style="{TdLabel}">Lieu</td><td style="{TdValue}">{meetingLocation}</td></tr>
            </table>
            <a href="{agendaUrl}" style="{BtnStyle}">Voir l&rsquo;ordre du jour</a>
            """);

    // ── Cotisations & paiements ──────────────────────────────────────────────

    public static string ChargeGenerated(string memberName, string typeName, decimal amount, string dueDate) =>
        Wrap("Nouvelle &eacute;ch&eacute;ance de cotisation",
            $"""
            <p>Bonjour <strong>{memberName}</strong>,</p>
            <p>Une nouvelle &eacute;ch&eacute;ance a &eacute;t&eacute; g&eacute;n&eacute;r&eacute;e :</p>
            <table style="width:100%;border-collapse:collapse;margin:16px 0">
              <tr><td style="{TdLabel}">Type</td><td style="{TdValue};font-weight:600">{typeName}</td></tr>
              <tr><td style="{TdLabel}">Montant d&ucirc;</td><td style="{TdValue}"><span style="{AmountStyle}">{amount:N0}</span></td></tr>
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
              <tr><td style="{TdLabel}">Montant</td><td style="{TdValue}"><span style="{AmountStyle}">{amount:N0}</span></td></tr>
              <tr><td style="{TdLabel}">Re&ccedil;u N&deg;</td><td style="{TdValue}"><span style="{BadgeOk}">{receiptNumber}</span></td></tr>
            </table>
            <p>Merci pour votre r&egrave;glement.</p>
            """);
}
