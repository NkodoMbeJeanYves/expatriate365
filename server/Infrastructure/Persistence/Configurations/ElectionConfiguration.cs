using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class ElectionConfiguration : IEntityTypeConfiguration<Election>
{
    public void Configure(EntityTypeBuilder<Election> b)
    {
        b.ToTable("elections");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        b.Property(e => e.Description).HasColumnName("description");
        b.Property(e => e.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        b.Property(e => e.StartDate).HasColumnName("start_date");
        b.Property(e => e.EndDate).HasColumnName("end_date");
        b.Property(e => e.MaxChoices).HasColumnName("max_choices").HasDefaultValue(1);
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(e => e.TenantId);
    }
}

public class ElectionCandidateConfiguration : IEntityTypeConfiguration<ElectionCandidate>
{
    public void Configure(EntityTypeBuilder<ElectionCandidate> b)
    {
        b.ToTable("election_candidates");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.ElectionId).HasColumnName("election_id").IsRequired();
        b.Property(e => e.MemberId).HasColumnName("member_id").IsRequired();
        b.Property(e => e.Statement).HasColumnName("statement");
        b.Property(e => e.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(e => new { e.ElectionId, e.MemberId }).IsUnique();
        b.HasOne(e => e.Election).WithMany(x => x.Candidates).HasForeignKey(e => e.ElectionId);
        b.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId);
    }
}

public class ElectionVoteConfiguration : IEntityTypeConfiguration<ElectionVote>
{
    public void Configure(EntityTypeBuilder<ElectionVote> b)
    {
        b.ToTable("election_votes");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.ElectionId).HasColumnName("election_id").IsRequired();
        b.Property(e => e.VoterId).HasColumnName("voter_id").IsRequired();
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(e => new { e.ElectionId, e.VoterId }).IsUnique();
        b.HasOne(e => e.Election).WithMany(x => x.Votes).HasForeignKey(e => e.ElectionId);
    }
}

public class ElectionVoteChoiceConfiguration : IEntityTypeConfiguration<ElectionVoteChoice>
{
    public void Configure(EntityTypeBuilder<ElectionVoteChoice> b)
    {
        b.ToTable("election_vote_choices");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.ElectionId).HasColumnName("election_id").IsRequired();
        b.Property(e => e.CandidateId).HasColumnName("candidate_id").IsRequired();
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(e => new { e.ElectionId, e.CandidateId });
        b.HasOne(e => e.Candidate).WithMany().HasForeignKey(e => e.CandidateId);
    }
}

public class ElectionBallotConfiguration : IEntityTypeConfiguration<ElectionBallot>
{
    public void Configure(EntityTypeBuilder<ElectionBallot> b)
    {
        b.ToTable("election_ballots");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.ElectionId).HasColumnName("election_id").IsRequired();
        b.Property(e => e.CandidateId).HasColumnName("candidate_id").IsRequired();
        b.Property(e => e.VoteCount).HasColumnName("vote_count").HasDefaultValue(0);
        b.Property(e => e.Rank).HasColumnName("rank").HasDefaultValue(0);
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(e => new { e.ElectionId, e.CandidateId }).IsUnique();
        b.HasOne(e => e.Election).WithMany(x => x.Ballots).HasForeignKey(e => e.ElectionId);
        b.HasOne(e => e.Candidate).WithMany().HasForeignKey(e => e.CandidateId);
    }
}
