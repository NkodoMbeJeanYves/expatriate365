using server.Application.Elections.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Tests;

/// <summary>
/// Tests that ListElectionsQuery correctly computes has_voted per user.
/// </summary>
public class ElectionHasVotedTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static AppDbContext SeedElectionWithVote(Guid voterId, bool voteIsActive = true)
    {
        var db = TestDbFactory.Create();
        var election = new Election
        {
            Id = Guid.NewGuid(), TenantId = TenantId,
            Title = "Élection du bureau", Status = "open", IsActive = true,
        };
        var vote = new ElectionVote
        {
            Id = Guid.NewGuid(), TenantId = TenantId,
            ElectionId = election.Id, VoterId = voterId, IsActive = voteIsActive,
            Election = election,
        };
        db.Elections.Add(election);
        db.ElectionVotes.Add(vote);
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task HasVoted_IsTrue_WhenUserHasAnActiveVote()
    {
        var userId = Guid.NewGuid();
        var db = SeedElectionWithVote(userId, voteIsActive: true);
        var handler = new ListElectionsQueryHandler(db);

        var result = await handler.Handle(
            new ListElectionsQuery(TenantId, userId, 1, 20, null, null),
            CancellationToken.None);

        Assert.Single(result.Data);
        Assert.True(result.Data.First().HasVoted);
    }

    [Fact]
    public async Task HasVoted_IsFalse_WhenUserHasNoVote()
    {
        var voterA = Guid.NewGuid();
        var voterB = Guid.NewGuid();          // this user did NOT vote
        var db = SeedElectionWithVote(voterA, voteIsActive: true);
        var handler = new ListElectionsQueryHandler(db);

        var result = await handler.Handle(
            new ListElectionsQuery(TenantId, voterB, 1, 20, null, null),
            CancellationToken.None);

        Assert.Single(result.Data);
        Assert.False(result.Data.First().HasVoted);
    }

    [Fact]
    public async Task HasVoted_IsFalse_WhenVoteIsInactive()
    {
        var userId = Guid.NewGuid();
        var db = SeedElectionWithVote(userId, voteIsActive: false);   // soft-deleted vote
        var handler = new ListElectionsQueryHandler(db);

        var result = await handler.Handle(
            new ListElectionsQuery(TenantId, userId, 1, 20, null, null),
            CancellationToken.None);

        Assert.Single(result.Data);
        Assert.False(result.Data.First().HasVoted);
    }

    [Fact]
    public async Task HasVoted_IsIsolatedPerUser_MultipleVoters()
    {
        var voterA = Guid.NewGuid();
        var voterB = Guid.NewGuid();
        var db = TestDbFactory.Create();
        var election = new Election
        {
            Id = Guid.NewGuid(), TenantId = TenantId,
            Title = "AG", Status = "open", IsActive = true,
        };
        db.Elections.Add(election);
        db.ElectionVotes.Add(new ElectionVote { Id = Guid.NewGuid(), TenantId = TenantId, ElectionId = election.Id, VoterId = voterA, IsActive = true, Election = election });
        db.SaveChanges();

        var handler = new ListElectionsQueryHandler(db);

        var resultA = await handler.Handle(new ListElectionsQuery(TenantId, voterA, 1, 20, null, null), CancellationToken.None);
        var resultB = await handler.Handle(new ListElectionsQuery(TenantId, voterB, 1, 20, null, null), CancellationToken.None);

        Assert.True(resultA.Data.First().HasVoted);
        Assert.False(resultB.Data.First().HasVoted);
    }
}
