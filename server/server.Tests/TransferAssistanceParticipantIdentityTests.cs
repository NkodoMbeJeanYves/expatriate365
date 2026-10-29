using Microsoft.Extensions.Logging.Abstractions;
using server.Application.TransferAssistance.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Tests;

public class TransferAssistanceParticipantIdentityTests
{
    [Fact]
    public async Task LegacyWitnessInvitationsAndAssignments_IdentifyEachRequestParticipant()
    {
        await using var db = TestDbFactory.Create();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test community", Slug = "identity-test" };
        db.Tenants.Add(tenant);

        var witness = AddMember(db, tenant.Id, "Witness", "Member", "M-W");
        var firstRequester = AddMember(db, tenant.Id, "Alex", "Requester", "M-R1");
        var firstHelper = AddMember(db, tenant.Id, "Jamie", "Helper", "M-H1");
        var secondRequester = AddMember(db, tenant.Id, "Morgan", "Requester", "M-R2");
        var secondHelper = AddMember(db, tenant.Id, "Taylor", "Helper", "M-H2");

        AddRequest(db, tenant.Id, firstRequester, firstHelper, witness);
        AddRequest(db, tenant.Id, secondRequester, secondHelper, witness);
        await db.SaveChangesAsync();

        var invitations = await new ListMyTransferAssistanceWitnessInvitationsHandler(
                db, NullLogger<ListMyTransferAssistanceWitnessInvitationsHandler>.Instance)
            .Handle(new(tenant.Id, witness.Id), default);
        var assignments = await new ListMyTransferAssistanceAssignmentsHandler(
                db, NullLogger<ListMyTransferAssistanceAssignmentsHandler>.Instance)
            .Handle(new(tenant.Id, witness.Id), default);

        Assert.Equal(2, invitations.Count);
        Assert.Contains(invitations, invitation =>
            invitation.RequesterName == "Alex Requester" &&
            invitation.IntermediaryName == "Jamie Helper" &&
            invitation.Purpose == "Family support" &&
            invitation.MeetingArea == "Ebene");
        Assert.Contains(invitations, invitation =>
            invitation.RequesterName == "Morgan Requester" && invitation.IntermediaryName == "Taylor Helper");
        Assert.Equal(2, assignments.Count);
        Assert.Contains(assignments, assignment =>
            assignment.RequesterName == "Alex Requester" &&
            assignment.IntermediaryName == "Jamie Helper" &&
            assignment.WitnessName == "Witness Member");
        Assert.Contains(assignments, assignment =>
            assignment.RequesterName == "Morgan Requester" &&
            assignment.IntermediaryName == "Taylor Helper" &&
            assignment.WitnessName == "Witness Member");
        var helperAssignments = await new ListMyTransferAssistanceAssignmentsHandler(
                db, NullLogger<ListMyTransferAssistanceAssignmentsHandler>.Instance)
            .Handle(new(tenant.Id, firstHelper.Id), default);
        var helperAssignment = Assert.Single(helperAssignments);
        Assert.Equal("Alex Requester", helperAssignment.RequesterName);
        Assert.Equal("Jamie Helper", helperAssignment.IntermediaryName);
        Assert.Equal("Witness Member", helperAssignment.WitnessName);
    }

    [Fact]
    public async Task RequesterRequestList_IdentifiesSelectedIntermediaryAndLegacyWitness()
    {
        await using var db = TestDbFactory.Create();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test community", Slug = "requester-identity-test" };
        db.Tenants.Add(tenant);
        var requester = AddMember(db, tenant.Id, "Alex", "Requester", "M-R");
        var helper = AddMember(db, tenant.Id, "Jamie", "Helper", "M-H");
        var witness = AddMember(db, tenant.Id, "Morgan", "Witness", "M-W");
        AddRequest(db, tenant.Id, requester, helper, witness);
        await db.SaveChangesAsync();

        var requests = await new ListMyTransferAssistanceRequestsHandler(db)
            .Handle(new(tenant.Id, requester.Id, 1, 20), default);

        var request = Assert.Single(requests.Data);
        Assert.Equal("Alex Requester", request.RequesterName);
        Assert.Equal("Jamie Helper", request.IntermediaryName);
        Assert.Equal("Morgan Witness", request.WitnessName);
    }

    private static Member AddMember(
        AppDbContext db,
        Guid tenantId,
        string firstName,
        string lastName,
        string membershipNumber)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = firstName,
            LastName = lastName,
            Email = $"{membershipNumber.ToLowerInvariant()}@example.test",
        };
        var member = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            User = user,
            MembershipNumber = membershipNumber,
            Status = "active",
            JoinedDate = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.Users.Add(user);
        db.Members.Add(member);
        return member;
    }

    private static void AddRequest(
        AppDbContext db,
        Guid tenantId,
        Member requester,
        Member helper,
        Member witness)
    {
        var request = new TransferAssistanceRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequesterMemberId = requester.Id,
            RequesterMember = requester,
            HelperMemberId = helper.Id,
            HelperMember = helper,
            WitnessMemberId = witness.Id,
            WitnessMember = witness,
            BeneficiaryName = "Beneficiary",
            DestinationCountry = "CM",
            AmountMur = 1000,
            ReceiveCurrency = "XAF",
            ReceiveMethod = "mobile_money",
            Purpose = "Family support",
            BeneficiaryRelationship = "Parent",
            AvailableFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            AvailableTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            MeetingArea = "Ebene",
            Status = TransferAssistanceStatuses.WitnessPending,
        };
        db.TransferAssistanceRequests.Add(request);
        db.TransferAssistanceWitnessInvitations.Add(new TransferAssistanceWitnessInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequestId = request.Id,
            Request = request,
            WitnessMemberId = witness.Id,
            WitnessMember = witness,
            InvitedByUserId = requester.UserId,
            Status = TransferAssistanceWitnessInvitationStatuses.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        });
    }
}
