using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence;

public static class RoleSeeder
{
    private static string P(params string[] perms) =>
        JsonSerializer.Serialize(perms);

    private static readonly string[] BureauBase =
    [
        Permissions.MembersRead, Permissions.MembersReadOwn, Permissions.MembersViewContact,
        Permissions.MembersUpdateOwn,
        Permissions.CategoriesRead,
        Permissions.ContributionsReadOwn,
        Permissions.PaymentsReadOwn, Permissions.PaymentsReceiptPrint,
        Permissions.VotesRead, Permissions.VotesCast, Permissions.VotesResults,
        Permissions.EventsRead, Permissions.EventsRegister,
        Permissions.DocumentsRead, Permissions.DocumentsUpload,
        Permissions.AnnouncementsRead,
        Permissions.NotificationsReadOwn,
        Permissions.DashboardRead,
        Permissions.SettingsRead,
    ];

    private static readonly (string name, string label, string description, string permissions)[] Roles =
    [
        (
            "super_admin",
            "Super Administrateur",
            "Accès complet à toutes les fonctionnalités",
            P(Permissions.All)
        ),
        (
            "president",
            "Président",
            "Responsable légal et représentant de l'association",
            P([
                ..BureauBase,
                Permissions.MembersCreate, Permissions.MembersUpdate,
                Permissions.MembersSendActivation, Permissions.MembersImport, Permissions.MembersExport,
                Permissions.CategoriesCreate, Permissions.CategoriesUpdate,
                Permissions.ContributionsRead, Permissions.ContributionsCreate, Permissions.ContributionsUpdate,
                Permissions.ContributionsValidate, Permissions.ContributionsExport,
                Permissions.PaymentsRead, Permissions.PaymentsCreate, Permissions.PaymentsValidate,
                Permissions.PaymentsExport,
                Permissions.VotesCreate, Permissions.VotesManage, Permissions.VotesDelete, Permissions.VotesExport,
                Permissions.EventsCreate, Permissions.EventsUpdate, Permissions.EventsDelete,
                Permissions.EventsManageAttendees, Permissions.EventsExport,
                Permissions.DocumentsManage, Permissions.DocumentsPublish,
                Permissions.AnnouncementsCreate, Permissions.AnnouncementsUpdate,
                Permissions.AnnouncementsDelete, Permissions.AnnouncementsPublish,
                Permissions.NotificationsSend,
                Permissions.DashboardFinancial, Permissions.DashboardMembers,
                Permissions.ReportsRead, Permissions.ReportsFinancial, Permissions.ReportsMembers,
                Permissions.ReportsContributions, Permissions.ReportsExport,
                Permissions.AuditRead,
                Permissions.SettingsUpdate, Permissions.SettingsNotifications,
                Permissions.RolesRead, Permissions.RolesAssign,
                Permissions.UsersRead, Permissions.UsersUpdate, Permissions.UsersResetPassword,
                Permissions.UsersImpersonate,
            ])
        ),
        (
            "vice_president",
            "Vice-Président",
            "Assiste et supplée le Président",
            P([
                ..BureauBase,
                Permissions.MembersCreate, Permissions.MembersUpdate,
                Permissions.MembersSendActivation, Permissions.MembersExport,
                Permissions.CategoriesCreate, Permissions.CategoriesUpdate,
                Permissions.ContributionsRead, Permissions.ContributionsCreate,
                Permissions.ContributionsValidate, Permissions.ContributionsExport,
                Permissions.PaymentsRead, Permissions.PaymentsCreate, Permissions.PaymentsValidate,
                Permissions.PaymentsExport,
                Permissions.VotesCreate, Permissions.VotesManage, Permissions.VotesExport,
                Permissions.EventsCreate, Permissions.EventsUpdate,
                Permissions.EventsManageAttendees, Permissions.EventsExport,
                Permissions.DocumentsManage, Permissions.DocumentsPublish,
                Permissions.AnnouncementsCreate, Permissions.AnnouncementsUpdate, Permissions.AnnouncementsPublish,
                Permissions.NotificationsSend,
                Permissions.DashboardFinancial, Permissions.DashboardMembers,
                Permissions.ReportsRead, Permissions.ReportsFinancial, Permissions.ReportsMembers,
                Permissions.ReportsContributions, Permissions.ReportsExport,
                Permissions.AuditRead,
                Permissions.RolesRead, Permissions.RolesAssign,
                Permissions.UsersRead, Permissions.UsersResetPassword,
                Permissions.UsersImpersonate,
            ])
        ),
        (
            "treasurer",
            "Trésorier",
            "Gestion des finances et de la comptabilité",
            P([
                ..BureauBase,
                Permissions.ContributionsRead, Permissions.ContributionsCreate, Permissions.ContributionsUpdate,
                Permissions.ContributionsDelete, Permissions.ContributionsValidate, Permissions.ContributionsExport,
                Permissions.ContributionsImport,
                Permissions.PaymentsRead, Permissions.PaymentsCreate, Permissions.PaymentsUpdate,
                Permissions.PaymentsDelete, Permissions.PaymentsValidate, Permissions.PaymentsRefund,
                Permissions.PaymentsExport,
                Permissions.DashboardFinancial,
                Permissions.ReportsRead, Permissions.ReportsFinancial, Permissions.ReportsContributions,
                Permissions.ReportsExport,
                Permissions.SettingsBilling,
                Permissions.UsersImpersonate,
            ])
        ),
        (
            "deputy_treasurer",
            "Trésorier Adjoint",
            "Assiste le Trésorier",
            P([
                ..BureauBase,
                Permissions.ContributionsRead, Permissions.ContributionsCreate, Permissions.ContributionsUpdate,
                Permissions.ContributionsValidate, Permissions.ContributionsExport,
                Permissions.PaymentsRead, Permissions.PaymentsCreate, Permissions.PaymentsValidate,
                Permissions.PaymentsExport,
                Permissions.DashboardFinancial,
                Permissions.ReportsRead, Permissions.ReportsFinancial, Permissions.ReportsContributions,
                Permissions.UsersImpersonate,
            ])
        ),
        (
            "secretary",
            "Secrétaire Général",
            "Gestion administrative et des communications",
            P([
                ..BureauBase,
                Permissions.MembersCreate, Permissions.MembersUpdate,
                Permissions.MembersSendActivation, Permissions.MembersImport, Permissions.MembersExport,
                Permissions.CategoriesCreate, Permissions.CategoriesUpdate,
                Permissions.ContributionsRead,
                Permissions.PaymentsRead, Permissions.PaymentsCreate, Permissions.PaymentsValidate,
                Permissions.VotesCreate, Permissions.VotesManage, Permissions.VotesExport,
                Permissions.EventsCreate, Permissions.EventsUpdate, Permissions.EventsManageAttendees,
                Permissions.EventsExport,
                Permissions.DocumentsManage, Permissions.DocumentsPublish,
                Permissions.AnnouncementsCreate, Permissions.AnnouncementsUpdate,
                Permissions.AnnouncementsDelete, Permissions.AnnouncementsPublish,
                Permissions.NotificationsSend,
                Permissions.DashboardMembers,
                Permissions.ReportsRead, Permissions.ReportsMembers,
                Permissions.SettingsNotifications,
                Permissions.RolesRead, Permissions.RolesAssign,
                Permissions.UsersRead, Permissions.UsersResetPassword,
                Permissions.UsersImpersonate,
            ])
        ),
        (
            "deputy_secretary",
            "Secrétaire Adjoint",
            "Assiste le Secrétaire Général",
            P([
                ..BureauBase,
                Permissions.MembersCreate, Permissions.MembersUpdate,
                Permissions.MembersSendActivation, Permissions.MembersExport,
                Permissions.CategoriesRead,
                Permissions.ContributionsRead,
                Permissions.PaymentsRead,
                Permissions.VotesCreate, Permissions.VotesManage,
                Permissions.EventsCreate, Permissions.EventsUpdate, Permissions.EventsManageAttendees,
                Permissions.DocumentsManage, Permissions.DocumentsPublish,
                Permissions.AnnouncementsCreate, Permissions.AnnouncementsUpdate, Permissions.AnnouncementsPublish,
                Permissions.NotificationsSend,
                Permissions.DashboardMembers,
                Permissions.ReportsRead, Permissions.ReportsMembers,
                Permissions.UsersImpersonate,
            ])
        ),
        (
            "auditor",
            "Commissaire aux Comptes",
            "Contrôle et certification des comptes",
            P([
                ..BureauBase,
                Permissions.ContributionsRead, Permissions.ContributionsExport,
                Permissions.PaymentsRead, Permissions.PaymentsExport,
                Permissions.DashboardFinancial,
                Permissions.ReportsRead, Permissions.ReportsFinancial, Permissions.ReportsMembers,
                Permissions.ReportsContributions, Permissions.ReportsExport,
                Permissions.AuditRead,
            ])
        ),
        (
            "deputy_auditor",
            "Adjoint Commissaire aux Comptes",
            "Assiste le Commissaire aux Comptes",
            P([
                ..BureauBase,
                Permissions.ContributionsRead, Permissions.ContributionsExport,
                Permissions.PaymentsRead,
                Permissions.DashboardFinancial,
                Permissions.ReportsRead, Permissions.ReportsFinancial, Permissions.ReportsContributions,
                Permissions.AuditRead,
            ])
        ),
        (
            "censor",
            "Censeur",
            "Surveillance et contrôle de la gestion",
            P([
                ..BureauBase,
                Permissions.ContributionsRead, Permissions.ContributionsExport,
                Permissions.PaymentsRead,
                Permissions.DashboardFinancial,
                Permissions.ReportsRead, Permissions.ReportsFinancial, Permissions.ReportsContributions,
                Permissions.AuditRead,
            ])
        ),
        (
            "committee_member",
            "Membre du Comité",
            "Membre du conseil d'administration",
            P([
                ..BureauBase,
                Permissions.DashboardMembers,
            ])
        ),
        (
            "regional_coordinator",
            "Coordinateur Régional",
            "Coordination des activités dans une région",
            P([
                ..BureauBase,
                Permissions.EventsCreate, Permissions.EventsUpdate, Permissions.EventsManageAttendees,
                Permissions.AnnouncementsCreate, Permissions.AnnouncementsUpdate,
            ])
        ),
        (
            "event_manager",
            "Gestionnaire Événements",
            "Organisation et gestion des événements",
            P([
                ..BureauBase,
                Permissions.EventsCreate, Permissions.EventsUpdate, Permissions.EventsDelete,
                Permissions.EventsManageAttendees, Permissions.EventsExport,
                Permissions.AnnouncementsCreate, Permissions.AnnouncementsUpdate,
            ])
        ),
        (
            "member",
            "Membre",
            "Membre ordinaire de l'association",
            P([
                Permissions.MembersReadOwn, Permissions.MembersUpdateOwn,
                Permissions.ContributionsReadOwn,
                Permissions.PaymentsReadOwn, Permissions.PaymentsReceiptPrint,
                Permissions.VotesRead, Permissions.VotesCast, Permissions.VotesResults,
                Permissions.EventsRead, Permissions.EventsRegister,
                Permissions.DocumentsRead,
                Permissions.AnnouncementsRead,
                Permissions.NotificationsReadOwn,
                Permissions.DashboardRead,
            ])
        ),
    ];

    public static async Task SeedRolesAsync(AppDbContext db)
    {
        foreach (var (name, label, description, permissions) in Roles)
        {
            var existing = await db.Roles.FirstOrDefaultAsync(r => r.Name == name);
            if (existing is null)
            {
                db.Roles.Add(new Role
                {
                    Id          = Guid.NewGuid(),
                    Name        = name,
                    Label       = label,
                    Description = description,
                    Permissions = permissions,
                    IsActive    = true,
                });
            }
            else
            {
                // Update permissions on existing roles so re-seeding propagates changes
                existing.Label       = label;
                existing.Description = description;
                existing.Permissions = permissions;
                existing.UpdatedAt   = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine("[Seeder] Roles seeded/updated.");
    }
}
