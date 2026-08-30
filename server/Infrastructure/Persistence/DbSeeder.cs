using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence;

public static class DbSeeder
{
    private const string SeedTenantSlug = "acm-camerounais-maurice";

    // ─────────────────────────────────────────────────────────────────────────
    // Entry point for --seed : DB already dropped+recreated by Program.cs
    // ─────────────────────────────────────────────────────────────────────────
    public static async Task ResetAndSeedAsync(AppDbContext db, IConfiguration config)
    {
        await RoleSeeder.SeedRolesAsync(db);
        await SeedSuperAdminAsync(db, config);
        await SeedDemoDataAsync(db);
        Console.WriteLine("[Seeder] Done.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Minimal bootstrap (always runs at startup, idempotent)
    // ─────────────────────────────────────────────────────────────────────────
    public static async Task BootstrapAsync(AppDbContext db, IConfiguration config)
    {
        await RoleSeeder.SeedRolesAsync(db);
        await SeedSuperAdminAsync(db, config);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Super admin — credentials from env (Seed__SuperAdminEmail / Seed__SuperAdminPassword)
    // ─────────────────────────────────────────────────────────────────────────
    public static async Task SeedSuperAdminAsync(AppDbContext db, IConfiguration config)
    {
        var email    = config["Seed:SuperAdminEmail"]    ?? "super_admin@expatriate365.mu";
        var password = config["Seed:SuperAdminPassword"] ?? "Admin@123";

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Role == "super_admin");
        if (existing is not null)
        {
            // Update credentials if they changed in env
            if (!existing.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                existing.Email = email;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] Super admin already exists — credentials ensured.");
            return;
        }

        var superAdmin = new User
        {
            Id              = Guid.NewGuid(),
            TenantId        = null,
            Email           = email,
            PasswordHash    = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName       = "Super",
            LastName        = "Admin",
            Role            = "super_admin",
            EmailVerifiedAt = DateTime.UtcNow,
            Status          = "active",
            IsActive        = true,
        };
        db.Users.Add(superAdmin);
        await db.SaveChangesAsync();
        Console.WriteLine($"[Seeder] Super admin created: {email}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Demo data — ACM Camerounais de Maurice
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedDemoDataAsync(AppDbContext db)
    {
        if (await db.Tenants.AnyAsync(t => t.Slug == SeedTenantSlug))
        {
            Console.WriteLine("[Seeder] Demo data already present — skipping.");
            return;
        }

        Console.WriteLine("[Seeder] Seeding ACM demo data...");

        // ── Tenant ────────────────────────────────────────────────────────────
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id                 = tenantId,
            Name               = "ACM - Association des Camerounais de Maurice",
            Slug               = SeedTenantSlug,
            BaseCurrency       = "EUR",
            CurrencySymbol     = "€",
            CountryCode        = "MU",
            SubscriptionTier   = "community",
            SubscriptionStatus = "active",
            IsActive           = true,
        };
        db.Tenants.Add(tenant);

        // ── Users ─────────────────────────────────────────────────────────────
        var pwdHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

        var uPresident  = MakeUser("jean.nkodo@acm.mu",    "Jean",    "Nkodo",   "president",  tenantId, pwdHash);
        var uTresorier  = MakeUser("marie.fotso@acm.mu",   "Marie",   "Fotso",   "treasurer",  tenantId, pwdHash);
        var uSecretaire = MakeUser("paul.mvondo@acm.mu",   "Paul",    "Mvondo",  "secretary",  tenantId, pwdHash);
        var uMember1    = MakeUser("alice.biya@acm.mu",    "Alice",   "Biya",    "member",     tenantId, pwdHash);
        var uMember2    = MakeUser("eric.manga@acm.mu",    "Eric",    "Manga",   "member",     tenantId, pwdHash);
        var uMember3    = MakeUser("solange.abah@acm.mu",  "Solange", "Abah",    "member",     tenantId, pwdHash);
        var uMember4    = MakeUser("boris.tchinda@acm.mu", "Boris",   "Tchinda", "member",     tenantId, pwdHash);
        var uMember5    = MakeUser("claire.nguele@acm.mu", "Claire",  "Nguele",  "member",     tenantId, pwdHash);
        db.Users.AddRange(uPresident, uTresorier, uSecretaire, uMember1, uMember2, uMember3, uMember4, uMember5);

        // ── Membership categories ─────────────────────────────────────────────
        var catOrd  = MakeCategory(tenantId, "Ordinaire",   "Membre ordinaire",               120m, true,  true);
        var catBien = MakeCategory(tenantId, "Bienfaiteur", "Membre bienfaiteur",             300m, true,  true);
        var catHon  = MakeCategory(tenantId, "Honoraire",   "Membre honoraire (sans cotis.)",   0m, true,  false);
        db.MembershipCategories.AddRange(catOrd, catBien, catHon);

        // ── Members ───────────────────────────────────────────────────────────
        var mPresident  = MakeMember(tenantId, uPresident.Id,  catOrd.Id,  "ACM-0001", "active",   new DateOnly(2020, 3, 15));
        var mTresorier  = MakeMember(tenantId, uTresorier.Id,  catBien.Id, "ACM-0002", "active",   new DateOnly(2020, 3, 15));
        var mSecretaire = MakeMember(tenantId, uSecretaire.Id, catOrd.Id,  "ACM-0003", "active",   new DateOnly(2021, 1, 10));
        var mMember1    = MakeMember(tenantId, uMember1.Id,    catOrd.Id,  "ACM-0004", "active",   new DateOnly(2021, 6, 1));
        var mMember2    = MakeMember(tenantId, uMember2.Id,    catOrd.Id,  "ACM-0005", "active",   new DateOnly(2022, 2, 20));
        var mMember3    = MakeMember(tenantId, uMember3.Id,    catBien.Id, "ACM-0006", "active",   new DateOnly(2022, 9, 5));
        var mMember4    = MakeMember(tenantId, uMember4.Id,    catOrd.Id,  "ACM-0007", "pending",  new DateOnly(2024, 11, 1));
        var mMember5    = MakeMember(tenantId, uMember5.Id,    catHon.Id,  "ACM-0008", "inactive", new DateOnly(2020, 3, 15));
        db.Members.AddRange(mPresident, mTresorier, mSecretaire, mMember1, mMember2, mMember3, mMember4, mMember5);

        // ── Contribution types ────────────────────────────────────────────────
        var ctAnnuel = new ContributionType
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Name = "Cotisation annuelle", Description = "Cotisation obligatoire annuelle",
            Frequency = "yearly", BaseAmount = 120m, LatePenaltyRate = 0.10m,
            GracePeriodDays = 30, EffectiveFrom = new DateOnly(2024, 1, 1), IsActive = true,
        };
        var ctSolidarite = new ContributionType
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Name = "Cotisation solidarité", Description = "Contribution au fonds de solidarité",
            Frequency = "yearly", BaseAmount = 50m, LatePenaltyRate = 0m,
            GracePeriodDays = 60, EffectiveFrom = new DateOnly(2024, 1, 1), IsActive = true,
        };
        db.ContributionTypes.AddRange(ctAnnuel, ctSolidarite);

        // ── Contribution charges ──────────────────────────────────────────────
        var activeMembers = new[] { mPresident, mTresorier, mSecretaire, mMember1, mMember2, mMember3 };
        var charges = new List<ContributionCharge>();
        foreach (var m in activeMembers)
        {
            var amount = m.CategoryId == catBien.Id ? catBien.ContributionRate : catOrd.ContributionRate;
            charges.Add(new ContributionCharge
            {
                Id = Guid.NewGuid(), TenantId = tenantId, MemberId = m.Id,
                ContributionTypeId = ctAnnuel.Id, DueDate = new DateOnly(2025, 3, 31),
                BaseAmount = amount, PenaltyAmount = 0m, WaiverAmount = 0m,
                AmountPaid = 0m, Status = "pending", IsActive = true,
            });
            charges.Add(new ContributionCharge
            {
                Id = Guid.NewGuid(), TenantId = tenantId, MemberId = m.Id,
                ContributionTypeId = ctSolidarite.Id, DueDate = new DateOnly(2025, 6, 30),
                BaseAmount = 50m, PenaltyAmount = 0m, WaiverAmount = 0m,
                AmountPaid = 0m, Status = "pending", IsActive = true,
            });
        }
        db.ContributionCharges.AddRange(charges);

        // ── Payments ──────────────────────────────────────────────────────────
        var seq = 1;
        foreach (var m in new[] { mPresident, mTresorier, mSecretaire, mMember1 })
        {
            var charge = charges.First(c => c.MemberId == m.Id && c.ContributionTypeId == ctAnnuel.Id);
            charge.AmountPaid = charge.BaseAmount;
            charge.Status = "paid";
            db.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(), TenantId = tenantId, MemberId = m.Id,
                ChargeId = charge.Id,
                ReceiptNumber = $"REC-2025-{seq++:D4}",
                Amount = charge.BaseAmount, Currency = "EUR",
                Status = "confirmed",
                ConfirmedAt = DateTime.UtcNow.AddDays(-seq * 7),
                ConfirmedBy = uTresorier.Id,
                PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-seq * 7)),
                Notes = "Paiement par virement", IsActive = true,
            });
        }

        // ── Welfare requests ──────────────────────────────────────────────────
        db.WelfareRequests.AddRange(
            new WelfareRequest
            {
                Id = Guid.NewGuid(), TenantId = tenantId, MemberId = mMember2.Id,
                Type = "medical", Description = "Frais hospitalisation suite à opération",
                AmountRequested = 800m, AmountApproved = 600m, AmountPaid = 600m,
                Status = "paid", ReviewedBy = uPresident.Id,
                ReviewedAt = DateTime.UtcNow.AddDays(-20), PaidAt = DateTime.UtcNow.AddDays(-15),
                PaidBy = uTresorier.Id, IsActive = true,
            },
            new WelfareRequest
            {
                Id = Guid.NewGuid(), TenantId = tenantId, MemberId = mMember3.Id,
                Type = "bereavement", Description = "Décès d'un parent au pays",
                AmountRequested = 500m, AmountApproved = 500m,
                Status = "approved", ReviewedBy = uPresident.Id,
                ReviewedAt = DateTime.UtcNow.AddDays(-5), IsActive = true,
            },
            new WelfareRequest
            {
                Id = Guid.NewGuid(), TenantId = tenantId, MemberId = mMember1.Id,
                Type = "education", Description = "Bourse scolaire enfant",
                AmountRequested = 300m, Status = "pending", IsActive = true,
            }
        );

        // ── Events ────────────────────────────────────────────────────────────
        var evtPasse = new Event
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Title = "Soirée culturelle camerounaise", Type = "cultural",
            Status = "completed", Location = "Salle Barclays, Port-Louis",
            StartDate = DateTime.UtcNow.AddDays(-45),
            EndDate = DateTime.UtcNow.AddDays(-45).AddHours(4),
            MaxCapacity = 80, IsPublic = true, IsActive = true,
        };
        var evtProchain = new Event
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Title = "Assemblée Générale Ordinaire 2025", Type = "meeting",
            Status = "published", Location = "Hôtel Le Suffren, Mahébourg",
            StartDate = DateTime.UtcNow.AddDays(15),
            EndDate = DateTime.UtcNow.AddDays(15).AddHours(3),
            MaxCapacity = 50, IsPublic = false, IsActive = true,
        };
        var evtFutur = new Event
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Title = "Fête nationale du Cameroun — Diaspora Maurice", Type = "celebration",
            Status = "published", Location = "Jardin de la Compagnie, Port-Louis",
            StartDate = DateTime.UtcNow.AddDays(60),
            EndDate = DateTime.UtcNow.AddDays(60).AddHours(6),
            MaxCapacity = 200, IsPublic = true, IsActive = true,
        };
        db.Events.AddRange(evtPasse, evtProchain, evtFutur);

        foreach (var m in new[] { mPresident, mTresorier, mSecretaire, mMember1, mMember2 })
            db.EventRegistrations.Add(new EventRegistration
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                EventId = evtPasse.Id, MemberId = m.Id,
                Status = "attended", AttendedAt = evtPasse.StartDate, IsActive = true,
            });
        foreach (var m in activeMembers)
            db.EventRegistrations.Add(new EventRegistration
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                EventId = evtProchain.Id, MemberId = m.Id,
                Status = "registered", IsActive = true,
            });

        // ── Meetings ──────────────────────────────────────────────────────────
        var meetingPasse = new Meeting
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Title = "Réunion du bureau — Mars 2025", Type = "board",
            Status = "completed",
            ScheduledAt = DateTime.UtcNow.AddDays(-30),
            StartedAt   = DateTime.UtcNow.AddDays(-30),
            EndedAt     = DateTime.UtcNow.AddDays(-30).AddHours(2),
            Location = "Siège ACM, Rose-Hill",
            Agenda = "1. Bilan financier\n2. Projets en cours\n3. Points divers",
            QuorumRequired = 3, IsActive = true,
        };
        var meetingProchain = new Meeting
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Title = "Réunion mensuelle du bureau — Juin 2025", Type = "board",
            Status = "scheduled",
            ScheduledAt = DateTime.UtcNow.AddDays(10),
            Location = "Siège ACM, Rose-Hill",
            Agenda = "1. Suivi cotisations\n2. Préparation AGO\n3. Événements à venir",
            QuorumRequired = 3, IsActive = true,
        };
        db.Meetings.AddRange(meetingPasse, meetingProchain);

        foreach (var m in new[] { mPresident, mTresorier, mSecretaire })
            db.MeetingAttendances.Add(new MeetingAttendance
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                MeetingId = meetingPasse.Id, MemberId = m.Id,
                Status = "present", IsActive = true,
            });
        db.MeetingAttendances.Add(new MeetingAttendance
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            MeetingId = meetingPasse.Id, MemberId = mMember1.Id,
            Status = "absent", IsActive = true,
        });

        // ── Elections ─────────────────────────────────────────────────────────
        var electionCloturee = new Election
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Title = "Élection du bureau 2024-2026",
            Description = "Renouvellement du bureau exécutif de l'ACM",
            Type = "board", Status = "results_published",
            StartDate = DateTime.UtcNow.AddDays(-60),
            EndDate = DateTime.UtcNow.AddDays(-45),
            MaxChoices = 1, IsActive = true,
        };
        var electionEnCours = new Election
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Title = "Vote — Projet activités culturelles 2025",
            Description = "Sélection du projet culturel prioritaire pour 2025",
            Type = "custom", Status = "open",
            StartDate = DateTime.UtcNow.AddDays(-3),
            EndDate = DateTime.UtcNow.AddDays(7),
            MaxChoices = 1, IsActive = true,
        };
        db.Elections.AddRange(electionCloturee, electionEnCours);

        var candPresident = new ElectionCandidate { Id = Guid.NewGuid(), TenantId = tenantId, ElectionId = electionCloturee.Id, MemberId = mPresident.Id,  Statement = "Je m'engage à renforcer les liens de notre communauté.", DisplayOrder = 1, IsActive = true };
        var candTresorier = new ElectionCandidate { Id = Guid.NewGuid(), TenantId = tenantId, ElectionId = electionCloturee.Id, MemberId = mTresorier.Id,  Statement = "Transparence et rigueur dans la gestion des finances.",    DisplayOrder = 2, IsActive = true };
        var candMember    = new ElectionCandidate { Id = Guid.NewGuid(), TenantId = tenantId, ElectionId = electionCloturee.Id, MemberId = mMember3.Id,    Statement = "Innovation et dynamisme pour notre association.",           DisplayOrder = 3, IsActive = true };
        db.ElectionCandidates.AddRange(candPresident, candTresorier, candMember);

        db.ElectionBallots.AddRange(
            new ElectionBallot { Id = Guid.NewGuid(), TenantId = tenantId, ElectionId = electionCloturee.Id, CandidateId = candPresident.Id, VoteCount = 4, Rank = 1, IsActive = true },
            new ElectionBallot { Id = Guid.NewGuid(), TenantId = tenantId, ElectionId = electionCloturee.Id, CandidateId = candTresorier.Id, VoteCount = 2, Rank = 2, IsActive = true },
            new ElectionBallot { Id = Guid.NewGuid(), TenantId = tenantId, ElectionId = electionCloturee.Id, CandidateId = candMember.Id,    VoteCount = 0, Rank = 3, IsActive = true }
        );
        foreach (var m in activeMembers)
            db.ElectionVotes.Add(new ElectionVote { Id = Guid.NewGuid(), TenantId = tenantId, ElectionId = electionCloturee.Id, VoterId = m.Id, IsActive = true });

        db.ElectionCandidates.AddRange(
            new ElectionCandidate { Id = Guid.NewGuid(), TenantId = tenantId, ElectionId = electionEnCours.Id, MemberId = mPresident.Id,  Statement = "Festival gastronomique camerounais",             DisplayOrder = 1, IsActive = true },
            new ElectionCandidate { Id = Guid.NewGuid(), TenantId = tenantId, ElectionId = electionEnCours.Id, MemberId = mSecretaire.Id, Statement = "Exposition photos — Histoire du Cameroun",        DisplayOrder = 2, IsActive = true }
        );

        // ── Communications ────────────────────────────────────────────────────
        var commSent = new Communication
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Title = "Rappel — Cotisations 2025",
            Content = "Chers membres,\n\nNous vous rappelons que les cotisations 2025 sont dues avant le 31 mars.\n\nLe Bureau de l'ACM",
            Type = "announcement", Channel = "app", Status = "sent",
            Audience = "all", SentAt = DateTime.UtcNow.AddDays(-10),
            RecipientCount = activeMembers.Length, IsActive = true,
        };
        var commDraft = new Communication
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Title = "Invitation — AGO 2025",
            Content = "Chers membres,\n\nVous êtes invités à l'Assemblée Générale Ordinaire de l'ACM.\n\nVotre présence est indispensable.",
            Type = "invitation", Channel = "app", Status = "draft",
            Audience = "all", RecipientCount = 0, IsActive = true,
        };
        db.Communications.AddRange(commSent, commDraft);
        foreach (var m in activeMembers)
            db.CommunicationRecipients.Add(new CommunicationRecipient
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                CommunicationId = commSent.Id, MemberId = m.Id,
                Status = "read", ReadAt = DateTime.UtcNow.AddDays(-9), IsActive = true,
            });

        // ── Documents ─────────────────────────────────────────────────────────
        db.Documents.AddRange(
            new Document { Id = Guid.NewGuid(), TenantId = tenantId, Title = "Statuts de l'ACM — Version 2024",    Description = "Statuts officiels",                       Type = "bylaw",  Category = "legal",          FileName = "statuts-acm-2024.pdf",         FileUrl = "/docs/statuts-acm-2024.pdf",        FileSizeBytes = 245_760, MimeType = "application/pdf", IsPublic = true, UploadedBy = uPresident.Id,  IsActive = true },
            new Document { Id = Guid.NewGuid(), TenantId = tenantId, Title = "Rapport d'activités 2024",            Description = "Bilan des activités 2024",               Type = "report", Category = "administrative", FileName = "rapport-activites-2024.pdf",   FileUrl = "/docs/rapport-2024.pdf",            FileSizeBytes = 512_000, MimeType = "application/pdf", IsPublic = true, UploadedBy = uSecretaire.Id, IsActive = true },
            new Document { Id = Guid.NewGuid(), TenantId = tenantId, Title = "Formulaire demande solidarité",        Description = "Formulaire aide sociale",                Type = "form",   Category = "administrative", FileName = "formulaire-solidarite.pdf",    FileUrl = "/docs/formulaire-solidarite.pdf",   FileSizeBytes = 98_304,  MimeType = "application/pdf", IsPublic = true, UploadedBy = uTresorier.Id,  IsActive = true }
        );

        // ── Board members ─────────────────────────────────────────────────────
        db.BoardMembers.AddRange(
            new BoardMember { Id = Guid.NewGuid(), TenantId = tenantId, MemberId = mPresident.Id,  Role = "Président",          StartDate = new DateOnly(2024, 3, 15), IsActive = true },
            new BoardMember { Id = Guid.NewGuid(), TenantId = tenantId, MemberId = mTresorier.Id,  Role = "Trésorière",         StartDate = new DateOnly(2024, 3, 15), IsActive = true },
            new BoardMember { Id = Guid.NewGuid(), TenantId = tenantId, MemberId = mSecretaire.Id, Role = "Secrétaire Général", StartDate = new DateOnly(2024, 3, 15), IsActive = true }
        );

        // ── Resolutions ───────────────────────────────────────────────────────
        db.Resolutions.AddRange(
            new Resolution { Id = Guid.NewGuid(), TenantId = tenantId, Title = "Approbation du budget 2025",     Content = "L'assemblée approuve le budget prévisionnel 2025 d'un montant de 4 500 € incluant les frais de fonctionnement, les activités culturelles et le fonds de solidarité.", Status = "adopted", AdoptedAt = new DateOnly(2024, 12, 15), VotesFor = 5, VotesAgainst = 0, Abstentions = 1, IsActive = true },
            new Resolution { Id = Guid.NewGuid(), TenantId = tenantId, Title = "Création d'un fonds de scolarité", Content = "Il est proposé de créer un fonds dédié aux bourses scolaires pour les enfants de membres en difficulté, doté initialement de 500 €.",                              Status = "draft",   VotesFor = 0, VotesAgainst = 0, Abstentions = 0,                            IsActive = true }
        );

        // ── Community posts ───────────────────────────────────────────────────
        var postAlice = new Post
        {
            Id = Guid.NewGuid(), TenantId = tenantId, AuthorId = mMember1.Id,
            Title   = "Mon premier mois à Maurice : ce que personne ne vous dit",
            Content = "Arrivée en janvier 2021, la première semaine a été un choc culturel total. " +
                      "Je m'attendais à un paradis tropical, mais j'ai d'abord trouvé la chaleur étouffante, " +
                      "les embouteillages de Port-Louis et la difficulté à trouver un logement abordable.\n\n" +
                      "Ce qui m'a sauvée, c'est la communauté camerounaise de l'ACM. Dès la deuxième semaine, " +
                      "Jean Nkodo m'a mise en contact avec une collègue qui cherchait une colocataire à Quatre-Bornes. " +
                      "C'est comme ça que j'ai trouvé mon appartement en 48h.\n\n" +
                      "Mon conseil n°1 : rejoignez l'ACM AVANT d'arriver. Le réseau est votre filet de sécurité.\n\n" +
                      "Mon conseil n°2 : apprenez quelques mots de créole mauricien. Les locaux apprécient énormément " +
                      "l'effort et ça ouvre des portes incroyables.\n\n" +
                      "Mon conseil n°3 : ouvrez votre compte bancaire dès le premier jour. La MCB et la SBM sont les " +
                      "plus pratiques pour les étrangers. Prévenez les délais — comptez 2 à 3 semaines.\n\n" +
                      "Quatre ans après, je ne regrette rien. Maurice est une chance extraordinaire pour qui sait " +
                      "s'y préparer. N'hésitez pas à me contacter si vous avez des questions !",
            Status = "published", PublishedAt = DateTime.UtcNow.AddDays(-45), IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-50),
        };

        var postEric = new Post
        {
            Id = Guid.NewGuid(), TenantId = tenantId, AuthorId = mMember2.Id,
            Title   = "Trouver un emploi dans le secteur IT à Maurice : mon parcours",
            Content = "Développeur fullstack avec 8 ans d'expérience en France, j'ai décidé de tenter l'aventure " +
                      "mauricienne en 2022. Voici comment j'ai décroché mon poste en 6 semaines.\n\n" +
                      "**Les secteurs qui recrutent :**\n" +
                      "- BPO/Centres d'appels (facile d'entrée mais salaires modestes)\n" +
                      "- Finance & FinTech (en plein boom depuis 2020)\n" +
                      "- Tourisme-tech (plateformes, réservations en ligne)\n" +
                      "- Administration publique (pour les postes spécialisés)\n\n" +
                      "**Ma méthode :**\n" +
                      "LinkedIn reste le meilleur canal. Les agences de recrutement locales comme Rogers et " +
                      "Manpower sont également efficaces. J'ai aussi contacté directement 15 entreprises via " +
                      "leurs sites, ce qui m'a valu 3 entretiens.\n\n" +
                      "**Le piège :** beaucoup d'offres mentionnent des salaires en roupies mauriciennes sans " +
                      "préciser le package (logement, transport, assurance santé). Négociez toujours le package " +
                      "complet, pas seulement le salaire brut.\n\n" +
                      "Actuellement chez une FinTech de Grand-Baie, je suis ravi de l'environnement de travail " +
                      "et du cadre de vie. Questions bienvenues !",
            Status = "published", PublishedAt = DateTime.UtcNow.AddDays(-20), IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-22),
        };

        var postSolange = new Post
        {
            Id = Guid.NewGuid(), TenantId = tenantId, AuthorId = mMember3.Id,
            Title   = "Scolariser ses enfants à Maurice : comparatif des écoles francophones",
            Content = "Maman de deux enfants (8 et 11 ans), j'ai visité 7 écoles avant de faire mon choix. " +
                      "Voici mon retour d'expérience pour les familles qui cherchent une scolarisation en français.\n\n" +
                      "Les établissements francophones reconnus :\n\n" +
                      "1. École française de Maurice (Moka) — programme AEFE, idéal si retour probable en France. " +
                      "Liste d'attente importante, inscrivez-vous 6 mois à l'avance.\n\n" +
                      "2. Lycée Labourdonnais (Mapou) — bilingue français/anglais, très bon niveau. " +
                      "Frais : environ 3 500 € par an.\n\n" +
                      "3. École du Sacré-Cœur (Quatre-Bornes) — catholique, francophone, excellente réputation. " +
                      "Plus abordable mais places limitées.\n\n" +
                      "Mon choix : Labourdonnais pour le bilinguisme. Mes enfants parlent maintenant couramment " +
                      "anglais et créole, ce qui est un atout énorme pour leur avenir.\n\n" +
                      "Je joins quelques photos des campus pour vous aider à visualiser.",
            Status = "published", PublishedAt = DateTime.UtcNow.AddDays(-8), IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
        };

        var postBoris = new Post
        {
            Id = Guid.NewGuid(), TenantId = tenantId, AuthorId = mMember4.Id,
            Title   = "Mon expérience avec le permis de résidence — ce qu'il faut anticiper",
            Content = "En cours de rédaction. Je partage bientôt les démarches pour l'Occupation Permit " +
                      "et le processus de renouvellement après 3 ans.",
            Status = "draft", IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
        };

        var postPresident = new Post
        {
            Id = Guid.NewGuid(), TenantId = tenantId, AuthorId = mPresident.Id,
            Title   = "Retour sur le festival camerounais 2024 — un succès !",
            Content = "Notre festival annuel a rassemblé plus de 200 personnes cette année. " +
                      "Un immense merci à tous les bénévoles et aux familles qui ont contribué. " +
                      "Les photos de l'événement sont disponibles ci-dessous.",
            Status = "rejected", IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
        };

        db.Posts.AddRange(postAlice, postEric, postSolange, postBoris, postPresident);

        // ── Post attachments ──────────────────────────────────────────────────
        db.PostAttachments.AddRange(
            new PostAttachment
            {
                Id = Guid.NewGuid(), PostId = postSolange.Id,
                FileUrl = "/avatars/ecole-labourdonnais.jpg",
                FileName = "ecole-labourdonnais.jpg", MimeType = "image/jpeg",
                FileSizeBytes = 184_320, AttachmentType = "photo",
                CreatedAt = postSolange.CreatedAt,
            },
            new PostAttachment
            {
                Id = Guid.NewGuid(), PostId = postSolange.Id,
                FileUrl = "/avatars/campus-sacre-coeur.jpg",
                FileName = "campus-sacre-coeur.jpg", MimeType = "image/jpeg",
                FileSizeBytes = 156_800, AttachmentType = "photo",
                CreatedAt = postSolange.CreatedAt,
            },
            new PostAttachment
            {
                Id = Guid.NewGuid(), PostId = postEric.Id,
                FileUrl = "/docs/guide-emploi-mauritius-2024.pdf",
                FileName = "guide-emploi-mauritius-2024.pdf", MimeType = "application/pdf",
                FileSizeBytes = 320_000, AttachmentType = "document",
                CreatedAt = postEric.CreatedAt,
            }
        );

        await db.SaveChangesAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static User MakeUser(string email, string first, string last, string role, Guid tenantId, string pwdHash) =>
        new() { Id = Guid.NewGuid(), TenantId = tenantId, Email = email, FirstName = first, LastName = last, Role = role, PasswordHash = pwdHash, Status = "active", EmailVerifiedAt = DateTime.UtcNow.AddDays(-30), IsActive = true };

    private static MembershipCategory MakeCategory(Guid tenantId, string name, string? desc, decimal rate, bool voting, bool welfare) =>
        new() { Id = Guid.NewGuid(), TenantId = tenantId, Name = name, Description = desc, ContributionRate = rate, VotingRights = voting, WelfareEligible = welfare, IsActive = true };

    private static Member MakeMember(Guid tenantId, Guid userId, Guid catId, string num, string status, DateOnly joined) =>
        new() { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, CategoryId = catId, MembershipNumber = num, Status = status, JoinedDate = joined, IsActive = status != "inactive" };
}
