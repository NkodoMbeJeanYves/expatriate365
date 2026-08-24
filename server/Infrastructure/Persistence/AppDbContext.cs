using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MembershipCategory> MembershipCategories => Set<MembershipCategory>();
    public DbSet<ContributionType> ContributionTypes => Set<ContributionType>();
    public DbSet<ContributionCharge> ContributionCharges => Set<ContributionCharge>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<WelfareRequest> WelfareRequests => Set<WelfareRequest>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<Communication> Communications => Set<Communication>();
    public DbSet<CommunicationRecipient> CommunicationRecipients => Set<CommunicationRecipient>();
    public DbSet<Election> Elections => Set<Election>();
    public DbSet<ElectionCandidate> ElectionCandidates => Set<ElectionCandidate>();
    public DbSet<ElectionVote> ElectionVotes => Set<ElectionVote>();
    public DbSet<ElectionBallot> ElectionBallots => Set<ElectionBallot>();
    public DbSet<ElectionVoteChoice> ElectionVoteChoices => Set<ElectionVoteChoice>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<Resolution> Resolutions => Set<Resolution>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingAttendance> MeetingAttendances => Set<MeetingAttendance>();
    public DbSet<MeetingMinute> MeetingMinutes => Set<MeetingMinute>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        StampUpdatedAt();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        StampUpdatedAt();
        return base.SaveChangesAsync(ct);
    }

    private void StampUpdatedAt()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (prop != null) prop.CurrentValue = DateTime.UtcNow;
            }
        }
    }
}
