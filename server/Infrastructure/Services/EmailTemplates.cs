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

    private static string Wrap(string title, string body, string lang = "fr") =>
        $"""
        <!DOCTYPE html><html lang="{lang}"><head><meta charset="UTF-8"/></head>
        <body style="font-family:Segoe UI,Arial,sans-serif;background:#f1f5f9;margin:0;padding:24px">
          <div style="{CardStyle}">
            <div style="{HeaderStyle}"><h1 style="{H1Style}">Expatriate365</h1></div>
            <div style="{BodyStyle}">
              <h2 style="margin-top:0;font-size:1.1rem;color:#111827">{title}</h2>
              {body}
            </div>
            <div style="{FooterStyle}">Expatriate365 &middot; {(lang == "en" ? "Do not reply to this email" : "Ne pas r&eacute;pondre &agrave; cet email")}</div>
          </div>
        </body></html>
        """;

    // ── Email subjects ───────────────────────────────────────────────────────

    public static class Subjects
    {
        public static string WelcomeOrgAdmin(string assocName, string lang) =>
            lang == "en" ? $"Welcome to Expatriate365 — {assocName}" : $"Bienvenue sur Expatriate365 — {assocName}";

        public static string MemberInvitation(string assocName, string lang) =>
            lang == "en" ? $"Invitation — {assocName}" : $"Activez votre compte — {assocName}";

        public static string PasswordReset(string lang) =>
            lang == "en" ? "Password reset" : "Réinitialisation de votre mot de passe";

        public static string EventInvite(string assocName, string title, string lang) =>
            lang == "en" ? $"[{assocName}] Event: {title}" : $"[{assocName}] Événement : {title}";

        public static string MeetingConvocation(string assocName, string title, string lang) =>
            lang == "en" ? $"[{assocName}] Notice of meeting: {title}" : $"[{assocName}] Convocation : {title}";

        public static string ChargeGenerated(string lang) =>
            lang == "en" ? "New contribution charge" : "Nouvelle échéance de cotisation";

        public static string PaymentConfirmed(string lang) =>
            lang == "en" ? "Payment confirmed" : "Paiement confirmé";
    }

    // ── Identifiants de connexion ────────────────────────────────────────────

    public static string WelcomeOrgAdmin(string fullName, string associationName, string email, string password, string loginUrl, string lang = "fr") =>
        lang == "en"
            ? Wrap($"Welcome to Expatriate365 &mdash; {associationName}",
                $"""
                <p>Hello <strong>{fullName}</strong>,</p>
                <p>Your association <strong>{associationName}</strong> has been created on Expatriate365.
                Here are your administrator credentials:</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0">
                  <tr><td style="{TdLabel}">Login email</td><td style="{TdValue}"><strong>{email}</strong></td></tr>
                  <tr><td style="{TdLabel}">Password</td><td style="{TdValue}"><strong>{password}</strong></td></tr>
                </table>
                <p style="color:#b45309;font-size:.85rem">&#9888; Please change your password on your first login.</p>
                <a href="{loginUrl}" style="{BtnStyle}">Sign in</a>
                """, "en")
            : Wrap($"Bienvenue sur Expatriate365 &mdash; {associationName}",
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

    public static string MemberInvitation(string fullName, string associationName, string setPasswordUrl, string lang = "fr") =>
        lang == "en"
            ? Wrap($"Invitation &mdash; {associationName}",
                $"""
                <p>Hello <strong>{fullName}</strong>,</p>
                <p>You have been invited to join the association <strong>{associationName}</strong> on Expatriate365.</p>
                <p>Click the button below to set your password and activate your account:</p>
                <a href="{setPasswordUrl}" style="{BtnStyle}">Activate my account</a>
                <p style="color:#94a3b8;font-size:.82rem;margin-top:16px">This link expires in 72 hours.</p>
                """, "en")
            : Wrap($"Invitation &mdash; {associationName}",
                $"""
                <p>Bonjour <strong>{fullName}</strong>,</p>
                <p>Vous avez &eacute;t&eacute; invit&eacute;(e) &agrave; rejoindre l&rsquo;association <strong>{associationName}</strong> sur Expatriate365.</p>
                <p>Cliquez sur le bouton ci-dessous pour d&eacute;finir votre mot de passe et activer votre compte :</p>
                <a href="{setPasswordUrl}" style="{BtnStyle}">Activer mon compte</a>
                <p style="color:#94a3b8;font-size:.82rem;margin-top:16px">Ce lien expire dans 72h.</p>
                """);

    public static string PasswordReset(string fullName, string resetUrl, string lang = "fr") =>
        lang == "en"
            ? Wrap("Password reset",
                $"""
                <p>Hello <strong>{fullName}</strong>,</p>
                <p>You have requested a password reset.</p>
                <a href="{resetUrl}" style="{BtnStyle}">Reset my password</a>
                <p style="color:#94a3b8;font-size:.82rem;margin-top:16px">This link expires in 1 hour. If you did not make this request, please ignore this email.</p>
                """, "en")
            : Wrap("R&eacute;initialisation de mot de passe",
                $"""
                <p>Bonjour <strong>{fullName}</strong>,</p>
                <p>Vous avez demand&eacute; la r&eacute;initialisation de votre mot de passe.</p>
                <a href="{resetUrl}" style="{BtnStyle}">R&eacute;initialiser mon mot de passe</a>
                <p style="color:#94a3b8;font-size:.82rem;margin-top:16px">Ce lien expire dans 1h. Si vous n&rsquo;avez pas fait cette demande, ignorez cet email.</p>
                """);

    // ── Événements & réunions ────────────────────────────────────────────────

    public static string EventInvite(string memberName, string eventTitle, string eventDate, string eventLocation, string associationName, string eventUrl, string lang = "fr") =>
        lang == "en"
            ? Wrap($"Event &mdash; {eventTitle}",
                $"""
                <p>Hello <strong>{memberName}</strong>,</p>
                <p>The association <strong>{associationName}</strong> is organising an event you are invited to attend:</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0">
                  <tr><td style="{TdLabel}">Event</td><td style="{TdValue}"><strong>{eventTitle}</strong></td></tr>
                  <tr><td style="{TdLabel}">Date</td><td style="{TdValue}"><span style="{BadgeInfo}">{eventDate}</span></td></tr>
                  <tr><td style="{TdLabel}">Location</td><td style="{TdValue}">{eventLocation}</td></tr>
                </table>
                <a href="{eventUrl}" style="{BtnStyle}">View event</a>
                """, "en")
            : Wrap($"&Eacute;v&eacute;nement &mdash; {eventTitle}",
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

    public static string MeetingConvocation(string memberName, string meetingTitle, string meetingDate, string meetingLocation, string associationName, string agendaUrl, string lang = "fr") =>
        lang == "en"
            ? Wrap($"Notice of meeting &mdash; {meetingTitle}",
                $"""
                <p>Hello <strong>{memberName}</strong>,</p>
                <p>You are invited to attend the following meeting of <strong>{associationName}</strong>:</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0">
                  <tr><td style="{TdLabel}">Meeting</td><td style="{TdValue}"><strong>{meetingTitle}</strong></td></tr>
                  <tr><td style="{TdLabel}">Date</td><td style="{TdValue}"><span style="{BadgeInfo}">{meetingDate}</span></td></tr>
                  <tr><td style="{TdLabel}">Location</td><td style="{TdValue}">{meetingLocation}</td></tr>
                </table>
                <a href="{agendaUrl}" style="{BtnStyle}">View agenda</a>
                """, "en")
            : Wrap($"Convocation &mdash; {meetingTitle}",
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

    public static string ChargeGenerated(string memberName, string typeName, decimal amount, string dueDate, string lang = "fr") =>
        lang == "en"
            ? Wrap("New contribution charge",
                $"""
                <p>Hello <strong>{memberName}</strong>,</p>
                <p>A new contribution charge has been generated for you:</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0">
                  <tr><td style="{TdLabel}">Type</td><td style="{TdValue};font-weight:600">{typeName}</td></tr>
                  <tr><td style="{TdLabel}">Amount due</td><td style="{TdValue}"><span style="{AmountStyle}">{amount:N0}</span></td></tr>
                  <tr><td style="{TdLabel}">Due date</td><td style="{TdValue}"><span style="{BadgeWarn}">{dueDate}</span></td></tr>
                </table>
                <p>Log in to your account to make your payment.</p>
                """, "en")
            : Wrap("Nouvelle &eacute;ch&eacute;ance de cotisation",
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

    public static string PaymentConfirmed(string memberName, string typeName, decimal amount, string receiptNumber, string lang = "fr") =>
        lang == "en"
            ? Wrap("Payment confirmed",
                $"""
                <p>Hello <strong>{memberName}</strong>,</p>
                <p>Your payment has been <strong>confirmed</strong> by the board:</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0">
                  <tr><td style="{TdLabel}">Contribution</td><td style="{TdValue};font-weight:600">{typeName}</td></tr>
                  <tr><td style="{TdLabel}">Amount</td><td style="{TdValue}"><span style="{AmountStyle}">{amount:N0}</span></td></tr>
                  <tr><td style="{TdLabel}">Receipt No.</td><td style="{TdValue}"><span style="{BadgeOk}">{receiptNumber}</span></td></tr>
                </table>
                <p>Thank you for your payment.</p>
                """, "en")
            : Wrap("Paiement confirm&eacute;",
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
