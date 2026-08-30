namespace server.Application.Common;

public static class Permissions
{
    // ── Members ──────────────────────────────────────────────────────────────
    public const string MembersRead           = "members.read";
    public const string MembersReadOwn        = "members.read_own";
    public const string MembersViewContact    = "members.view_contact";
    public const string MembersCreate         = "members.create";
    public const string MembersUpdate         = "members.update";
    public const string MembersUpdateOwn      = "members.update_own";
    public const string MembersDelete         = "members.delete";
    public const string MembersSendActivation = "members.send_activation";
    public const string MembersImport         = "members.import";
    public const string MembersExport         = "members.export";

    // ── Member Categories ─────────────────────────────────────────────────────
    public const string CategoriesRead   = "categories.read";
    public const string CategoriesCreate = "categories.create";
    public const string CategoriesUpdate = "categories.update";
    public const string CategoriesDelete = "categories.delete";

    // ── Contributions ────────────────────────────────────────────────────────
    public const string ContributionsRead     = "contributions.read";
    public const string ContributionsReadOwn  = "contributions.read_own";
    public const string ContributionsCreate   = "contributions.create";
    public const string ContributionsUpdate   = "contributions.update";
    public const string ContributionsDelete   = "contributions.delete";
    public const string ContributionsValidate = "contributions.validate";
    public const string ContributionsExport   = "contributions.export";
    public const string ContributionsImport   = "contributions.import";

    // ── Payments ─────────────────────────────────────────────────────────────
    public const string PaymentsRead         = "payments.read";
    public const string PaymentsReadOwn      = "payments.read_own";
    public const string PaymentsCreate       = "payments.create";
    public const string PaymentsUpdate       = "payments.update";
    public const string PaymentsDelete       = "payments.delete";
    public const string PaymentsValidate     = "payments.validate";
    public const string PaymentsRefund       = "payments.refund";
    public const string PaymentsExport       = "payments.export";
    public const string PaymentsReceiptPrint = "payments.receipt.print";

    // ── Votes ────────────────────────────────────────────────────────────────
    public const string VotesRead    = "votes.read";
    public const string VotesCast    = "votes.cast";
    public const string VotesResults = "votes.results";
    public const string VotesCreate  = "votes.create";
    public const string VotesManage  = "votes.manage";
    public const string VotesDelete  = "votes.delete";
    public const string VotesExport  = "votes.export";

    // ── Events ───────────────────────────────────────────────────────────────
    public const string EventsRead            = "events.read";
    public const string EventsRegister        = "events.register";
    public const string EventsCreate          = "events.create";
    public const string EventsUpdate          = "events.update";
    public const string EventsDelete          = "events.delete";
    public const string EventsManageAttendees = "events.manage_attendees";
    public const string EventsExport          = "events.export";

    // ── Documents ────────────────────────────────────────────────────────────
    public const string DocumentsRead    = "documents.read";
    public const string DocumentsUpload  = "documents.upload";
    public const string DocumentsManage  = "documents.manage";
    public const string DocumentsPublish = "documents.publish";

    // ── Community ────────────────────────────────────────────────────────────
    public const string CommunityRead     = "community.read";
    public const string CommunityWrite    = "community.write";
    public const string CommunityModerate = "community.moderate";

    // ── Announcements ────────────────────────────────────────────────────────
    public const string AnnouncementsRead    = "announcements.read";
    public const string AnnouncementsCreate  = "announcements.create";
    public const string AnnouncementsUpdate  = "announcements.update";
    public const string AnnouncementsDelete  = "announcements.delete";
    public const string AnnouncementsPublish = "announcements.publish";

    // ── Notifications ────────────────────────────────────────────────────────
    public const string NotificationsReadOwn = "notifications.read_own";
    public const string NotificationsSend    = "notifications.send";
    public const string NotificationsManage  = "notifications.manage";

    // ── Dashboard & Reports ──────────────────────────────────────────────────
    public const string DashboardRead          = "dashboard.read";
    public const string DashboardFinancial     = "dashboard.financial";
    public const string DashboardMembers       = "dashboard.members";
    public const string ReportsRead            = "reports.read";
    public const string ReportsFinancial       = "reports.financial";
    public const string ReportsMembers         = "reports.members";
    public const string ReportsContributions   = "reports.contributions";
    public const string ReportsExport          = "reports.export";
    public const string AuditRead              = "audit.read";

    // ── Settings ─────────────────────────────────────────────────────────────
    public const string SettingsRead          = "settings.read";
    public const string SettingsUpdate        = "settings.update";
    public const string SettingsBilling       = "settings.billing";
    public const string SettingsNotifications = "settings.notifications";
    public const string SettingsIntegrations  = "settings.integrations";

    // ── Roles ────────────────────────────────────────────────────────────────
    public const string RolesRead   = "roles.read";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesDelete = "roles.delete";
    public const string RolesAssign = "roles.assign";

    // ── Users ────────────────────────────────────────────────────────────────
    public const string UsersRead          = "users.read";
    public const string UsersCreate        = "users.create";
    public const string UsersUpdate        = "users.update";
    public const string UsersDelete        = "users.delete";
    public const string UsersResetPassword = "users.reset_password";
    public const string UsersImpersonate   = "users.impersonate";

    // ── Groupes par domaine (pour le formulaire UI) ───────────────────────────
    public static readonly IReadOnlyDictionary<string, string[]> ByDomain =
        new Dictionary<string, string[]>
        {
            ["members"]       = [MembersRead, MembersReadOwn, MembersViewContact, MembersCreate, MembersUpdate, MembersUpdateOwn, MembersDelete, MembersSendActivation, MembersImport, MembersExport],
            ["categories"]    = [CategoriesRead, CategoriesCreate, CategoriesUpdate, CategoriesDelete],
            ["contributions"] = [ContributionsRead, ContributionsReadOwn, ContributionsCreate, ContributionsUpdate, ContributionsDelete, ContributionsValidate, ContributionsExport, ContributionsImport],
            ["payments"]      = [PaymentsRead, PaymentsReadOwn, PaymentsCreate, PaymentsUpdate, PaymentsDelete, PaymentsValidate, PaymentsRefund, PaymentsExport, PaymentsReceiptPrint],
            ["votes"]         = [VotesRead, VotesCast, VotesResults, VotesCreate, VotesManage, VotesDelete, VotesExport],
            ["events"]        = [EventsRead, EventsRegister, EventsCreate, EventsUpdate, EventsDelete, EventsManageAttendees, EventsExport],
            ["documents"]     = [DocumentsRead, DocumentsUpload, DocumentsManage, DocumentsPublish],
            ["community"]     = [CommunityRead, CommunityWrite, CommunityModerate],
            ["announcements"] = [AnnouncementsRead, AnnouncementsCreate, AnnouncementsUpdate, AnnouncementsDelete, AnnouncementsPublish],
            ["notifications"] = [NotificationsReadOwn, NotificationsSend, NotificationsManage],
            ["dashboard"]     = [DashboardRead, DashboardFinancial, DashboardMembers, ReportsRead, ReportsFinancial, ReportsMembers, ReportsContributions, ReportsExport, AuditRead],
            ["settings"]      = [SettingsRead, SettingsUpdate, SettingsBilling, SettingsNotifications, SettingsIntegrations],
            ["roles"]         = [RolesRead, RolesCreate, RolesUpdate, RolesDelete, RolesAssign],
            ["users"]         = [UsersRead, UsersCreate, UsersUpdate, UsersDelete, UsersResetPassword, UsersImpersonate],
        };

    public static string[] All => ByDomain.Values.SelectMany(p => p).ToArray();
}
