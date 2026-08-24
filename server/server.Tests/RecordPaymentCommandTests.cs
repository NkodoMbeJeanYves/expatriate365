using Microsoft.Extensions.Logging.Abstractions;
using server.Application.Payments.Commands;
using server.Application.Payments.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Tests;

/// <summary>
/// Tests RG-I3-002: payment cannot exceed the remaining charge balance.
/// </summary>
public class RecordPaymentCommandTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (AppDbContext, ContributionCharge) SeedCharge(decimal totalDue, decimal alreadyPaid = 0)
    {
        var db = TestDbFactory.Create();

        var user = new User { Id = Guid.NewGuid(), TenantId = TenantId, Email = "a@b.com", FirstName = "Jean", LastName = "Doe", PasswordHash = "x" };
        var member = new Member { Id = Guid.NewGuid(), TenantId = TenantId, UserId = user.Id, MembershipNumber = "MBR-0001", JoinedDate = DateOnly.FromDateTime(DateTime.Today), User = user };
        var type = new ContributionType { Id = Guid.NewGuid(), TenantId = TenantId, Name = "Cotisation", Frequency = "annual", BaseAmount = totalDue, EffectiveFrom = DateOnly.FromDateTime(DateTime.Today) };
        var charge = new ContributionCharge
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            MemberId = member.Id,
            ContributionTypeId = type.Id,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            BaseAmount = totalDue,
            AmountPaid = alreadyPaid,
            Member = member,
            ContributionType = type,
        };

        db.Users.Add(user);
        db.Members.Add(member);
        db.ContributionTypes.Add(type);
        db.ContributionCharges.Add(charge);
        db.SaveChanges();
        return (db, charge);
    }

    [Fact]
    public async Task Handle_ReturnsFailed_WhenAmountExceedsBalance()
    {
        var (db, charge) = SeedCharge(totalDue: 10_000, alreadyPaid: 6_000);
        var handler = new RecordPaymentCommandHandler(db, NullLogger<RecordPaymentCommandHandler>.Instance);

        var cmd = new RecordPaymentCommand(TenantId,
            new RecordPaymentRequest(charge.Id.ToString(), 5_000, "cash", DateTime.Today.ToString("yyyy-MM-dd"), null));

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("dépasse le solde", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenAmountEqualsBalance()
    {
        var (db, charge) = SeedCharge(totalDue: 10_000, alreadyPaid: 0);
        var handler = new RecordPaymentCommandHandler(db, NullLogger<RecordPaymentCommandHandler>.Instance);

        var cmd = new RecordPaymentCommand(TenantId,
            new RecordPaymentRequest(charge.Id.ToString(), 10_000, "cash", DateTime.Today.ToString("yyyy-MM-dd"), null));

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ReturnsFailed_WhenChargeAlreadySettled()
    {
        var (db, charge) = SeedCharge(totalDue: 10_000, alreadyPaid: 10_000);
        var handler = new RecordPaymentCommandHandler(db, NullLogger<RecordPaymentCommandHandler>.Instance);

        var cmd = new RecordPaymentCommand(TenantId,
            new RecordPaymentRequest(charge.Id.ToString(), 1_000, "cash", DateTime.Today.ToString("yyyy-MM-dd"), null));

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("soldée", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ReturnsFailed_WhenPaymentMethodInvalid()
    {
        var (db, charge) = SeedCharge(totalDue: 10_000);
        var handler = new RecordPaymentCommandHandler(db, NullLogger<RecordPaymentCommandHandler>.Instance);

        var cmd = new RecordPaymentCommand(TenantId,
            new RecordPaymentRequest(charge.Id.ToString(), 1_000, "bitcoin", DateTime.Today.ToString("yyyy-MM-dd"), null));

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Méthode de paiement", result.ErrorMessage);
    }
}
