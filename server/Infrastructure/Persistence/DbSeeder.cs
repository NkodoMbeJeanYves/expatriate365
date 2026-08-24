using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using BCrypt.Net;

namespace server.Infrastructure.Persistence;

public static class DbSeeder
{
    private const string SeedTenantSlug = "acm-camerounais-maurice";

    public static async Task ReseedAsync(AppDbContext db)
    {
        Console.WriteLine("[Seeder] --reseed: removing all data in FK order...");
        db.ElectionVoteChoices.RemoveRange(db.ElectionVoteChoices);
        db.ElectionBallots.RemoveRange(db.ElectionBallots);
        db.ElectionVotes.RemoveRange(db.ElectionVotes);
        db.ElectionCandidates.RemoveRange(db.ElectionCandidates);
        db.Elections.RemoveRange(db.Elections);
        db.MeetingAttendances.RemoveRange(db.MeetingAttendances);
        db.MeetingMinutes.RemoveRange(db.MeetingMinutes);
        db.Meetings.RemoveRange(db.Meetings);
        db.EventRegistrations.RemoveRange(db.EventRegistrations);
        db.Events.RemoveRange(db.Events);
        db.Payments.RemoveRange(db.Payments);
        db.ContributionCharges.RemoveRange(db.ContributionCharges);
        db.ContributionTypes.RemoveRange(db.ContributionTypes);
        db.CommunicationRecipients.RemoveRange(db.CommunicationRecipients);
        db.Communications.RemoveRange(db.Communications);
        db.WelfareRequests.RemoveRange(db.WelfareRequests);
        db.BoardMembers.RemoveRange(db.BoardMembers);
        db.Resolutions.RemoveRange(db.Resolutions);
        db.Documents.RemoveRange(db.Documents);
        db.Members.RemoveRange(db.Members);
        db.MembershipCategories.RemoveRange(db.MembershipCategories);
        db.Users.RemoveRange(db.Users);
        db.Tenants.RemoveRange(db.Tenants);
        await db.SaveChangesAsync();
        Console.WriteLine("[Seeder] All data removed. Re-seeding...");
        await SeedAsync(db);
    }

    public static async Task SeedAsync(AppDbContext db)
    {
        // Idempotent : skip entirely if seed tenant already exists
        if (await db.Tenants.AnyAsync(t => t.Slug == SeedTenantSlug))
        {
            Console.WriteLine("[Seeder] Data already present — skipping.");
            return;
        }
        Console.WriteLine("[Seeder] Seeding ACM data...");

        // ── Tenant ────────────────────────────────────────────────────────────
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "ACM - Association des Camerounais de Maurice",
            Slug = "acm-camerounais-maurice",
            BaseCurrency = "XAF",
            CurrencySymbol = "FCFA",
            CountryCode = "MU",
            SubscriptionTier = "community",
            SubscriptionStatus = "active",
            IsActive = true,
        };
        db.Tenants.Add(tenant);

        // ── Users ─────────────────────────────────────────────────────────────
        var pwdHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

        var uPresident  = User("jean.nkodo@acm.mu",    "Jean",     "Nkodo",     "president",  tenantId, pwdHash);
        var uTresorier  = User("marie.fotso@acm.mu",   "Marie",    "Fotso",     "treasurer",  tenantId, pwdHash);
        var uSecretaire = User("paul.mvondo@acm.mu",   "Paul",     "Mvondo",    "secretary",  tenantId, pwdHash);
        var uMember1    = User("alice.biya@acm.mu",    "Alice",    "Biya",      "member",     tenantId, pwdHash);
        var uMember2    = User("eric.manga@acm.mu",    "Eric",     "Manga",     "member",     tenantId, pwdHash);
        var uMember3    = User("solange.abah@acm.mu",  "Solange",  "Abah",      "member",     tenantId, pwdHash);
        var uMember4    = User("boris.tchinda@acm.mu", "Boris",    "Tchinda",   "member",     tenantId, pwdHash);
        var uMember5    = User("claire.nguele@acm.mu", "Claire",   "Nguele",    "member",     tenantId, pwdHash);

        var users = new[] { uPresident, uTresorier, uSecretaire, uMember1, uMember2, uMember3, uMember4, uMember5 };
        db.Users.AddRange(users);

        // ── Membership categories ─────────────────────────────────────────────
        var catOrd  = Category(tenantId, "Ordinaire",    "Membre ordinaire",               120m, true,  true);
        var catBien = Category(tenantId, "Bienfaiteur",  "Membre bienfaiteur",             300m, true,  true);
        var catHon  = Category(tenantId, "Honoraire",    "Membre honoraire (sans cotis.)",   0m, true,  false);
        db.MembershipCategories.AddRange(catOrd, catBien, catHon);

        // ── Members ───────────────────────────────────────────────────────────
        var mPresident  = Member(tenantId, uPresident.Id,  catOrd.Id,  "ACM-0001", "active",   new DateOnly(2020, 3, 15));
        var mTresorier  = Member(tenantId, uTresorier.Id,  catBien.Id, "ACM-0002", "active",   new DateOnly(2020, 3, 15));
        var mSecretaire = Member(tenantId, uSecretaire.Id, catOrd.Id,  "ACM-0003", "active",   new DateOnly(2021, 1, 10));
        var mMember1    = Member(tenantId, uMember1.Id,    catOrd.Id,  "ACM-0004", "active",   new DateOnly(2021, 6, 1));
        var mMember2    = Member(tenantId, uMember2.Id,    catOrd.Id,  "ACM-0005", "active",   new DateOnly(2022, 2, 20));
        var mMember3    = Member(tenantId, uMember3.Id,    catBien.Id, "ACM-0006", "active",   new DateOnly(2022, 9, 5));
        var mMember4    = Member(tenantId, uMember4.Id,    catOrd.Id,  "ACM-0007", "pending",  new DateOnly(2024, 11, 1));
        var mMember5    = Member(tenantId, uMember5.Id,    catHon.Id,  "ACM-0008", "inactive", new DateOnly(2020, 3, 15));

        var members = new[] { mPresident, mTresorier, mSecretaire, mMember1, mMember2, mMember3, mMember4, mMember5 };
        db.Members.AddRange(members);

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
            var cat = m.CategoryId == catBien.Id ? catBien : catOrd;
            var amount = cat.ContributionRate;

            // Cotisation annuelle
            var chAnnuel = new ContributionCharge
            {
                Id = Guid.NewGuid(), TenantId = tenantId, MemberId = m.Id,
                ContributionTypeId = ctAnnuel.Id, DueDate = new DateOnly(2025, 3, 31),
                BaseAmount = amount, PenaltyAmount = 0m, WaiverAmount = 0m,
                AmountPaid = 0m, Status = "pending", IsActive = true,
            };
            // Cotisation solidarité
            var chSol = new ContributionCharge
            {
                Id = Guid.NewGuid(), TenantId = tenantId, MemberId = m.Id,
                ContributionTypeId = ctSolidarite.Id, DueDate = new DateOnly(2025, 6, 30),
                BaseAmount = 50m, PenaltyAmount = 0m, WaiverAmount = 0m,
                AmountPaid = 0m, Status = "pending", IsActive = true,
            };
            charges.AddRange([chAnnuel, chSol]);
        }
        db.ContributionCharges.AddRange(charges);

        // ── Payments (60% des charges annuelles payées) ───────────────────────
        var payments = new List<Payment>();
        var paidMembers = new[] { mPresident, mTresorier, mSecretaire, mMember1 };
        var receiptSeq = 1;

        foreach (var m in paidMembers)
        {
            var charge = charges.First(c => c.MemberId == m.Id && c.ContributionTypeId == ctAnnuel.Id);
            charge.AmountPaid = charge.BaseAmount;
            charge.Status = "paid";

            var payment = new Payment
            {
                Id = Guid.NewGuid(), TenantId = tenantId, MemberId = m.Id,
                ChargeId = charge.Id,
                ReceiptNumber = $"REC-2025-{receiptSeq++:D4}",
                Amount = charge.BaseAmount, Currency = "EUR",
                Status = "confirmed",
                ConfirmedAt = DateTime.UtcNow.AddDays(-receiptSeq * 7),
                ConfirmedBy = uTresorier.Id,
                PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-receiptSeq * 7)),
                Notes = "Paiement par virement", IsActive = true,
            };
            payments.Add(payment);
        }
        db.Payments.AddRange(payments);

        // ── Welfare requests ──────────────────────────────────────────────────
        var welfare1 = new WelfareRequest
        {
            Id = Guid.NewGuid(), TenantId = tenantId, MemberId = mMember2.Id,
            Type = "medical", Description = "Frais hospitalisation suite à opération",
            AmountRequested = 800m, AmountApproved = 600m, AmountPaid = 600m,
            Status = "paid", ReviewedBy = uPresident.Id,
            ReviewedAt = DateTime.UtcNow.AddDays(-20), PaidAt = DateTime.UtcNow.AddDays(-15),
            PaidBy = uTresorier.Id, IsActive = true,
        };
        var welfare2 = new WelfareRequest
        {
            Id = Guid.NewGuid(), TenantId = tenantId, MemberId = mMember3.Id,
            Type = "bereavement", Description = "Décès d'un parent au pays",
            AmountRequested = 500m, AmountApproved = 500m,
            Status = "approved", ReviewedBy = uPresident.Id,
            ReviewedAt = DateTime.UtcNow.AddDays(-5), IsActive = true,
        };
        var welfare3 = new WelfareRequest
        {
            Id = Guid.NewGuid(), TenantId = tenantId, MemberId = mMember1.Id,
            Type = "education", Description = "Bourse scolaire enfant",
            AmountRequested = 300m, Status = "pending", IsActive = true,
        };
        db.WelfareRequests.AddRange(welfare1, welfare2, welfare3);

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

        // Registrations pour l'événement passé
        var regMembers = new[] { mPresident, mTresorier, mSecretaire, mMember1, mMember2 };
        foreach (var m in regMembers)
        {
            db.EventRegistrations.Add(new EventRegistration
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                EventId = evtPasse.Id, MemberId = m.Id,
                Status = "attended", AttendedAt = evtPasse.StartDate, IsActive = true,
            });
        }
        // Registrations pour l'AGO
        foreach (var m in activeMembers)
        {
            db.EventRegistrations.Add(new EventRegistration
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                EventId = evtProchain.Id, MemberId = m.Id,
                Status = "registered", IsActive = true,
            });
        }

        // ── Meetings ──────────────────────────────────────────────────────────
        var meetingPasse = new Meeting
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Title = "Réunion du bureau — Mars 2025", Type = "board",
            Status = "completed",
            ScheduledAt = DateTime.UtcNow.AddDays(-30),
            StartedAt = DateTime.UtcNow.AddDays(-30),
            EndedAt = DateTime.UtcNow.AddDays(-30).AddHours(2),
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

        // Présences réunion passée
        var boardMembers = new[] { mPresident, mTresorier, mSecretaire };
        foreach (var m in boardMembers)
        {
            db.MeetingAttendances.Add(new MeetingAttendance
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                MeetingId = meetingPasse.Id, MemberId = m.Id,
                Status = "present", IsActive = true,
            });
        }
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

        // Candidats élection clôturée
        var candPresident = new ElectionCandidate
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            ElectionId = electionCloturee.Id, MemberId = mPresident.Id,
            Statement = "Je m'engage à renforcer les liens de notre communauté.",
            DisplayOrder = 1, IsActive = true,
        };
        var candTresorier = new ElectionCandidate
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            ElectionId = electionCloturee.Id, MemberId = mTresorier.Id,
            Statement = "Transparence et rigueur dans la gestion des finances.",
            DisplayOrder = 2, IsActive = true,
        };
        var candMember = new ElectionCandidate
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            ElectionId = electionCloturee.Id, MemberId = mMember3.Id,
            Statement = "Innovation et dynamisme pour notre association.",
            DisplayOrder = 3, IsActive = true,
        };
        db.ElectionCandidates.AddRange(candPresident, candTresorier, candMember);

        // Résultats (ballots)
        db.ElectionBallots.AddRange(
            new ElectionBallot { Id = Guid.NewGuid(), TenantId = tenantId, ElectionId = electionCloturee.Id, CandidateId = candPresident.Id,  VoteCount = 4, Rank = 1, IsActive = true },
            new ElectionBallot { Id = Guid.NewGuid(), TenantId = tenantId, ElectionId = electionCloturee.Id, CandidateId = candTresorier.Id,  VoteCount = 2, Rank = 2, IsActive = true },
            new ElectionBallot { Id = Guid.NewGuid(), TenantId = tenantId, ElectionId = electionCloturee.Id, CandidateId = candMember.Id,      VoteCount = 0, Rank = 3, IsActive = true }
        );

        // Votes (anonymes — juste le fait d'avoir voté)
        foreach (var m in new[] { mPresident, mTresorier, mSecretaire, mMember1, mMember2, mMember3 })
        {
            db.ElectionVotes.Add(new ElectionVote
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                ElectionId = electionCloturee.Id, VoterId = m.Id, IsActive = true,
            });
        }

        // Candidats élection en cours
        var optFestival = new ElectionCandidate
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            ElectionId = electionEnCours.Id, MemberId = mPresident.Id,
            Statement = "Festival gastronomique camerounais", DisplayOrder = 1, IsActive = true,
        };
        var optExpo = new ElectionCandidate
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            ElectionId = electionEnCours.Id, MemberId = mSecretaire.Id,
            Statement = "Exposition photos — Histoire du Cameroun", DisplayOrder = 2, IsActive = true,
        };
        db.ElectionCandidates.AddRange(optFestival, optExpo);

        // ── Communications ────────────────────────────────────────────────────
        var commSent = new Communication
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Title = "Rappel — Cotisations 2025",
            Content = "Chers membres,\n\nNous vous rappelons que les cotisations 2025 sont dues avant le 31 mars. Merci de régulariser votre situation.\n\nLe Bureau de l'ACM",
            Type = "announcement", Channel = "app", Status = "sent",
            Audience = "all", SentAt = DateTime.UtcNow.AddDays(-10),
            RecipientCount = activeMembers.Length, IsActive = true,
        };
        var commDraft = new Communication
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Title = "Invitation — AGO 2025",
            Content = "Chers membres,\n\nVous êtes invités à l'Assemblée Générale Ordinaire de l'ACM qui se tiendra le [DATE] à [LIEU].\n\nVotre présence est indispensable.",
            Type = "invitation", Channel = "app", Status = "draft",
            Audience = "all", RecipientCount = 0, IsActive = true,
        };
        db.Communications.AddRange(commSent, commDraft);

        // Destinataires communication envoyée
        foreach (var m in activeMembers)
        {
            db.CommunicationRecipients.Add(new CommunicationRecipient
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                CommunicationId = commSent.Id, MemberId = m.Id,
                Status = "read", ReadAt = DateTime.UtcNow.AddDays(-9), IsActive = true,
            });
        }

        // ── Documents ─────────────────────────────────────────────────────────
        db.Documents.AddRange(
            new Document
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                Title = "Statuts de l'ACM — Version 2024",
                Description = "Statuts officiels de l'Association des Camerounais de Maurice",
                Type = "bylaw", Category = "legal",
                FileName = "statuts-acm-2024.pdf", FileUrl = "/docs/statuts-acm-2024.pdf",
                FileSizeBytes = 245_760, MimeType = "application/pdf",
                IsPublic = true, UploadedBy = uPresident.Id, IsActive = true,
            },
            new Document
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                Title = "Rapport d'activités 2024",
                Description = "Bilan des activités de l'association pour l'année 2024",
                Type = "report", Category = "administrative",
                FileName = "rapport-activites-2024.pdf", FileUrl = "/docs/rapport-2024.pdf",
                FileSizeBytes = 512_000, MimeType = "application/pdf",
                IsPublic = true, UploadedBy = uSecretaire.Id, IsActive = true,
            },
            new Document
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                Title = "Formulaire demande solidarité",
                Description = "Formulaire à compléter pour toute demande d'aide sociale",
                Type = "form", Category = "administrative",
                FileName = "formulaire-solidarite.pdf", FileUrl = "/docs/formulaire-solidarite.pdf",
                FileSizeBytes = 98_304, MimeType = "application/pdf",
                IsPublic = true, UploadedBy = uTresorier.Id, IsActive = true,
            }
        );

        // ── Board members ─────────────────────────────────────────────────────
        db.BoardMembers.AddRange(
            new BoardMember { Id = Guid.NewGuid(), TenantId = tenantId, MemberId = mPresident.Id,  Role = "Président",          StartDate = new DateOnly(2024, 3, 15), IsActive = true },
            new BoardMember { Id = Guid.NewGuid(), TenantId = tenantId, MemberId = mTresorier.Id,  Role = "Trésorière",         StartDate = new DateOnly(2024, 3, 15), IsActive = true },
            new BoardMember { Id = Guid.NewGuid(), TenantId = tenantId, MemberId = mSecretaire.Id, Role = "Secrétaire Général", StartDate = new DateOnly(2024, 3, 15), IsActive = true }
        );

        // ── Resolutions ───────────────────────────────────────────────────────
        db.Resolutions.AddRange(
            new Resolution
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                Title = "Approbation du budget 2025",
                Content = "L'assemblée approuve le budget prévisionnel 2025 d'un montant de 4 500 € incluant les frais de fonctionnement, les activités culturelles et le fonds de solidarité.",
                Status = "adopted", AdoptedAt = new DateOnly(2024, 12, 15),
                VotesFor = 5, VotesAgainst = 0, Abstentions = 1, IsActive = true,
            },
            new Resolution
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                Title = "Création d'un fonds de scolarité",
                Content = "Il est proposé de créer un fonds dédié aux bourses scolaires pour les enfants de membres en difficulté, doté initialement de 500 €.",
                Status = "draft", VotesFor = 0, VotesAgainst = 0, Abstentions = 0, IsActive = true,
            }
        );

        await db.SaveChangesAsync();
        Console.WriteLine("[Seeder] Done.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static User User(string email, string first, string last, string role, Guid tenantId, string pwdHash) =>
        new()
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Email = email,
            FirstName = first, LastName = last, Role = role,
            PasswordHash = pwdHash, Status = "active",
            EmailVerifiedAt = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
        };

    private static MembershipCategory Category(Guid tenantId, string name, string? desc, decimal rate, bool voting, bool welfare) =>
        new()
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Name = name,
            Description = desc, ContributionRate = rate,
            VotingRights = voting, WelfareEligible = welfare, IsActive = true,
        };

    private static Member Member(Guid tenantId, Guid userId, Guid catId, string num, string status, DateOnly joined) =>
        new()
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            CategoryId = catId, MembershipNumber = num, Status = status,
            JoinedDate = joined, IsActive = status != "inactive",
        };
}
