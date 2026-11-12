using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using server.Application.Common;
using server.Application.Elections.Commands;
using server.Application.Elections.DTOs;
using server.Application.Elections.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Tests;

public class ElectionCandidateReviewTests
{
    [Fact]
    public async Task ApproveCandidate_StoresReviewerAndDecisionTimestamp()
    {
        using var db = TestDbFactory.Create();
        var (candidate, tenantId, reviewerId, _) = await SeedCandidate(db);
        var handler = new ReviewElectionCandidateCommandHandler(
            db, NullLogger<ReviewElectionCandidateCommandHandler>.Instance);

        var result = await handler.Handle(
            new ReviewElectionCandidateCommand(
                tenantId, candidate.ElectionId, candidate.Id, reviewerId,
                new ReviewElectionCandidateRequest("approved", null)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("approved", candidate.Status);
        Assert.Equal(reviewerId, candidate.ReviewedBy);
        Assert.NotNull(candidate.ReviewedAt);
        Assert.Null(candidate.ReviewReason);
    }

    [Fact]
    public async Task RejectCandidate_RequiresAndStoresReason()
    {
        using var db = TestDbFactory.Create();
        var (candidate, tenantId, reviewerId, _) = await SeedCandidate(db);
        var handler = new ReviewElectionCandidateCommandHandler(
            db, NullLogger<ReviewElectionCandidateCommandHandler>.Instance);

        var missingReason = await handler.Handle(
            new ReviewElectionCandidateCommand(
                tenantId, candidate.ElectionId, candidate.Id, reviewerId,
                new ReviewElectionCandidateRequest("rejected", " ")),
            CancellationToken.None);

        Assert.False(missingReason.IsSuccess);
        Assert.Equal("errors.candidate.review_reason_required", missingReason.ErrorCode);
        Assert.Equal("pending", candidate.Status);

        var rejected = await handler.Handle(
            new ReviewElectionCandidateCommand(
                tenantId, candidate.ElectionId, candidate.Id, reviewerId,
                new ReviewElectionCandidateRequest("rejected", "Eligibility requirements not met")),
            CancellationToken.None);

        Assert.True(rejected.IsSuccess);
        Assert.Equal("rejected", candidate.Status);
        Assert.Equal("Eligibility requirements not met", candidate.ReviewReason);
        Assert.Equal(reviewerId, candidate.ReviewedBy);
        Assert.NotNull(candidate.ReviewedAt);
    }

    [Fact]
    public async Task CandidateCannotReviewTheirOwnCandidacy()
    {
        using var db = TestDbFactory.Create();
        var (candidate, tenantId, _, applicantId) = await SeedCandidate(db);
        var handler = new ReviewElectionCandidateCommandHandler(
            db, NullLogger<ReviewElectionCandidateCommandHandler>.Instance);

        var result = await handler.Handle(
            new ReviewElectionCandidateCommand(
                tenantId, candidate.ElectionId, candidate.Id, applicantId,
                new ReviewElectionCandidateRequest("approved", null)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("errors.candidate.cannot_review_own", result.ErrorCode);
        Assert.Equal("pending", candidate.Status);
    }

    [Fact]
    public async Task ElectionDetails_HidePendingCandidatesUnlessReviewerPermissionIsPresent()
    {
        using var db = TestDbFactory.Create();
        var (candidate, tenantId, _, _) = await SeedCandidate(db);
        var handler = new GetElectionByIdQueryHandler(db);

        var publicResult = await handler.Handle(
            new GetElectionByIdQuery(tenantId, candidate.ElectionId),
            CancellationToken.None);
        var reviewerResult = await handler.Handle(
            new GetElectionByIdQuery(tenantId, candidate.ElectionId, IncludePendingCandidates: true),
            CancellationToken.None);

        Assert.True(publicResult.IsSuccess);
        Assert.Empty(publicResult.Data!.Item2);
        Assert.True(reviewerResult.IsSuccess);
        Assert.Single(reviewerResult.Data!.Item2);
        Assert.Equal("pending", reviewerResult.Data.Item2[0].Status);
    }

    [Fact]
    public void ElectionCommissionRole_IncludesMemberAndCandidateReviewPermissions()
    {
        var json = RoleSeeder.GetDefaultPermissions(Roles.ElectionCommissionMember);
        var permissions = JsonSerializer.Deserialize<string[]>(json!);
        var memberJson = RoleSeeder.GetDefaultPermissions(Roles.Member);
        var memberPermissions = JsonSerializer.Deserialize<string[]>(memberJson!);

        Assert.NotNull(permissions);
        Assert.NotNull(memberPermissions);
        Assert.All(memberPermissions, permission => Assert.Contains(permission, permissions));
        Assert.Contains(Permissions.VotesCandidatesReview, permissions);
        Assert.DoesNotContain(Permissions.VotesManage, permissions);
        Assert.DoesNotContain(Permissions.VotesCreate, permissions);
    }

    [Fact]
    public async Task DemoSeeder_AddsCommissionMembersAndIsIdempotent()
    {
        await using var db = TestDbFactory.Create();
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();

        await DbSeeder.ResetAndSeedAsync(db, config);
        var legacyCommissionUser = db.Users.Single(user =>
            user.Email == "yebila.david.njafuh@acm.mu");
        legacyCommissionUser.Email = "yebila.david.njafuh@example.test";
        await db.SaveChangesAsync();
        await DbSeeder.BootstrapAsync(db, config);

        var commissionUsers = db.Users
            .Where(user => user.Role == Roles.ElectionCommissionMember)
            .OrderBy(user => user.Email)
            .ToList();
        Assert.Equal(5, commissionUsers.Count);
        Assert.All(commissionUsers, user => Assert.EndsWith("@acm.mu", user.Email));
        Assert.All(commissionUsers, user => Assert.False(string.IsNullOrWhiteSpace(user.Phone)));
        var tenantId = db.Tenants.Single(tenant => tenant.Slug == "acm-camerounais-maurice").Id;
        Assert.Equal(13, db.Members.Count(member => member.TenantId == tenantId));
        Assert.Contains(db.Users, user =>
            user.Email == "jean.nkodo@acm.mu" && user.Role == Roles.OrgAdmin);
    }

    [Fact]
    public async Task DemoSeeder_CreatesOnlyDraftExecutiveBoardElection()
    {
        await using var db = TestDbFactory.Create();
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();

        await DbSeeder.ResetAndSeedAsync(db, config);

        var election = Assert.Single(db.Elections);
        Assert.Equal(db.Tenants.Single(tenant => tenant.Slug == "acm-camerounais-maurice").Id, election.TenantId);
        Assert.Equal("Élection du bureau exécutif ACM 2026–2028", election.Title);
        Assert.Equal("election des membres du bureau executif de l'ACM", election.Description);
        Assert.Equal("board", election.Type);
        Assert.Equal("draft", election.Status);
        Assert.Equal(new DateTime(2026, 10, 18), election.StartDate);
        Assert.Equal(new DateTime(2026, 12, 19), election.EndDate);
        Assert.Equal(1, election.MaxChoices);
        Assert.True(election.IsActive);
        Assert.Empty(db.ElectionCandidates);
        Assert.Empty(db.ElectionVotes);
        Assert.Empty(db.ElectionVoteChoices);
        Assert.Empty(db.ElectionBallots);
    }

    private static async Task<(ElectionCandidate Candidate, Guid TenantId, Guid ReviewerId, Guid ApplicantId)> SeedCandidate(
        AppDbContext db)
    {
        var tenantId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var applicant = new User
        {
            Id = Guid.NewGuid(),
            Email = $"applicant-{Guid.NewGuid():N}@example.test",
            FirstName = "Candidate",
            LastName = "Member",
        };
        var member = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = applicant.Id,
            User = applicant,
            MembershipNumber = "MEM-0001",
        };
        var election = new Election
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = "Board Election",
            Status = "draft",
        };
        var candidate = new ElectionCandidate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ElectionId = election.Id,
            MemberId = member.Id,
            Election = election,
            Member = member,
        };

        db.Users.Add(applicant);
        db.Members.Add(member);
        db.Elections.Add(election);
        db.ElectionCandidates.Add(candidate);
        await db.SaveChangesAsync();

        return (candidate, tenantId, reviewerId, applicant.Id);
    }
}
