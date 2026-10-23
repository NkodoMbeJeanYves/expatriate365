using System.Security.Claims;
using System.Text.Json;
using MediatR;
using server.Application.Common;
using server.Application.TransferAssistance.Commands;
using server.Application.TransferAssistance.DTOs;
using server.Application.TransferAssistance.Queries;
using server.Domain.Entities;

namespace server.API.TransferAssistance;

public static class TransferAssistanceEndpoints
{
    public static void MapTransferAssistanceEndpoints(this IEndpointRouteBuilder app)
    {
        var requests = app.MapGroup("/api/v1/transfer-requests")
            .WithTags("Transfer Assistance")
            .RequireAuthorization();

        var profiles = app.MapGroup("/api/v1/transfer-assistance/profile")
            .WithTags("Transfer Assistance Profile")
            .RequireAuthorization(Permissions.TransferAssistanceProfileManage);

        app.MapGet("/api/v1/transfer-assistance/rules", async (
                ClaimsPrincipal principal, IMediator mediator, CancellationToken ct) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();
                return Results.Ok(await mediator.Send(
                    new GetTransferAssistanceRulesQuery(tenantId.Value, memberId.Value), ct));
            })
            .WithTags("Transfer Assistance Profile")
            .RequireAuthorization(Permissions.TransferAssistanceProfileManage)
            .WithSummary("Get helper/witness participation rules and the current member's eligibility")
            .WithDescription("""
                Read-only rules mirrored from the server configuration, used by the information page.
                verifier_roles lists the association roles allowed to review accreditations.
                Sample response: {"require_mfa":false,"matching_window_days":14,"invitation_window_days":5,
                "minimum_request_amount":1,"maximum_request_amount":0,"amount_currency":"MUR","requests_open":false,
                "verifier_roles":["org_admin","auditor"],"my_eligibility":{"member_active":true,"email_verified":true,
                "mfa_enabled":false,"is_community_admin":false,"can_request_accreditation":true}}.
                """)
            .Produces<TransferAssistanceRulesResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        profiles.MapGet("/me", async (ClaimsPrincipal principal, IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();
                var profile = await mediator.Send(
                    new GetMyTransferAssistanceProfileQuery(tenantId.Value, memberId.Value));
                return Results.Ok(profile);
            })
            .WithSummary("Get the current member's helper and witness opt-in status")
            .Produces<TransferAssistanceProfileResponse>();

        profiles.MapPut("/me", async (
                ClaimsPrincipal principal,
                IMediator mediator,
                UpdateTransferAssistanceProfileRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new UpdateTransferAssistanceProfileCommand(
                    tenantId.Value, memberId.Value, userId.Value, request));
                return result.IsSuccess
                    ? Results.Ok(result.Data)
                    : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .WithSummary("Request, cancel or withdraw a helper or witness accreditation")
            .WithDescription("""
                Submit {"participant_role":"helper","action":"request"} to request accreditation,
                or {"participant_role":"witness","action":"cancel"} to cancel or withdraw it.
                A non-cancelled accreditation cannot be requested again. Community administrators
                cannot request accreditation. Existing assignments are preserved on withdrawal.
                Legacy helper_opted_in/witness_opted_in payloads remain supported.
                Sample response: {"helper_opted_in":true,"helper_verification_status":"pending","witness_opted_in":false,"witness_verification_status":"not_requested"}.
                Sample error: {"error":"errors.transfer_assistance.accreditation_exists"}.
                """)
            .Produces<TransferAssistanceProfileResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        requests.MapGet("/available", async (ClaimsPrincipal principal, IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();
                return Results.Ok(await mediator.Send(
                    new ListAvailableTransferAssistanceRequestsQuery(tenantId.Value, memberId.Value)));
            })
            .RequireAuthorization(Permissions.TransferAssistanceViewAvailable)
            .WithSummary("List anonymized matching requests available to verified helpers")
            .Produces<IReadOnlyList<TransferAssistanceAvailableRequestResponse>>();

        requests.MapGet("/sync-state", async (ClaimsPrincipal principal, HttpContext http, IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                if (tenantId is null) return Results.Unauthorized();
                http.Response.Headers.CacheControl = "no-store";
                return Results.Ok(await mediator.Send(new GetTransferAssistanceSyncStateQuery(
                    tenantId.Value, GetMemberId(principal), GetPermissions(principal))));
            })
            .WithSummary("Get lightweight change tokens for the transfer assistance sections visible to the caller")
            .WithDescription("""
                Poll this endpoint and reload only the sections whose token changed.
                Sections the caller is not permitted to see are omitted.
                Sample response: {"version":"9f1c2a7b0d3e4f51","sections":{"mine":"a1b2c3d4e5f60718","available":"0f9e8d7c6b5a4932","assignments":"5e4d3c2b1a0f9e8d","witness_invitations":"1a2b3c4d5e6f7081","beneficiary_open":"c0ffee1234567890"},"server_time":"2026-10-23T08:15:00Z"}
                """)
            .Produces<TransferAssistanceSyncStateResponse>();

        requests.MapGet("/beneficiary-open", async (ClaimsPrincipal principal, IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();
                return Results.Ok(await mediator.Send(
                    new ListBeneficiaryOpenTransferAssistanceRequestsQuery(tenantId.Value, memberId.Value)));
            })
            .RequireAuthorization(Permissions.TransferAssistanceReadOwn)
            .WithSummary("List anonymized requests still looking for a beneficiary")
            .WithDescription("""
                The requester identity is never exposed. my_candidate_status is null when the caller has not applied.
                Sample response: [{"id":"3f0e…","destination_country":"CM","amount_mur":15000,"receive_currency":"XAF","receive_method":"mobile_money","purpose":"Hospital fees","available_from":"2026-10-24","available_to":"2026-10-31","expires_at":"2026-10-30T10:00:00Z","my_candidate_status":"pending","my_candidate_id":"8a1d…"}]
                """)
            .Produces<IReadOnlyList<TransferAssistanceBeneficiaryOpenRequestResponse>>();

        requests.MapPost("/{id:guid}/beneficiary-candidates", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                ApplyTransferAssistanceBeneficiaryRequest? request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new ApplyTransferAssistanceBeneficiaryCommand(
                    tenantId.Value, memberId.Value, userId.Value, id,
                    request ?? new ApplyTransferAssistanceBeneficiaryRequest(null)));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceReadOwn)
            .WithSummary("Apply to become the beneficiary of a request without a beneficiary")
            .WithDescription("""
                Idempotent while the candidacy is pending; a withdrawn candidacy can be renewed.
                Sample response: {"id":"8a1d…","request_id":"3f0e…","member_id":"77c2…","member_name":"Marie Ngo","message":"I can receive the funds for the hospital.","status":"pending","created_at":"2026-10-23T08:15:00Z"}
                Sample error: {"error":"errors.transfer_assistance.beneficiary_candidate_conflict"}
                """)
            .Produces<TransferAssistanceBeneficiaryCandidateResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapDelete("/{id:guid}/beneficiary-candidates/me", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new WithdrawTransferAssistanceBeneficiaryCommand(
                    tenantId.Value, memberId.Value, userId.Value, id));
                return result.IsSuccess ? Results.NoContent() : WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceReadOwn)
            .WithSummary("Withdraw the caller's pending beneficiary candidacy")
            .WithDescription("""
                Returns 204 No Content. Sample error: {"error":"errors.transfer_assistance.invalid_transition"}
                """)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapGet("/{id:guid}/beneficiary-candidates", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();
                var result = await mediator.Send(new ListTransferAssistanceBeneficiaryCandidatesQuery(
                    tenantId.Value, memberId.Value, id));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceReadOwn)
            .WithSummary("List beneficiary candidates for a request owned by the caller, oldest first")
            .WithDescription("""
                Sample response: [{"id":"8a1d…","request_id":"3f0e…","member_id":"77c2…","member_name":"Marie Ngo","message":null,"status":"pending","created_at":"2026-10-23T08:15:00Z"}]
                """)
            .Produces<IReadOnlyList<TransferAssistanceBeneficiaryCandidateResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        requests.MapPost("/{id:guid}/beneficiary", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                DesignateTransferAssistanceBeneficiaryRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new DesignateTransferAssistanceBeneficiaryCommand(
                    tenantId.Value, memberId.Value, userId.Value, id, request));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceReadOwn)
            .WithSummary("Designate the beneficiary of an owned request from a candidate or a member")
            .WithDescription("""
                Provide exactly one of candidate_id or member_id. Idempotent for the same member.
                Sample request: {"candidate_id":"8a1d…","beneficiary_relationship":"Friend"}
                Sample response: the updated transfer request with beneficiary_pending=false.
                Sample error: {"error":"errors.transfer_assistance.beneficiary_already_designated"}
                """)
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapGet("/assignments", async (ClaimsPrincipal principal, IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();
                return Results.Ok(await mediator.Send(
                    new ListMyTransferAssistanceAssignmentsQuery(tenantId.Value, memberId.Value)));
            })
            .RequireAuthorization(Permissions.TransferAssistanceParticipate)
            .WithSummary("List the current member's helper and witness assignments")
            .Produces<IReadOnlyList<TransferAssistanceAssignmentResponse>>();

        requests.MapGet("/witness-invitations/me", async (ClaimsPrincipal principal, IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();
                return Results.Ok(await mediator.Send(
                    new ListMyTransferAssistanceWitnessInvitationsQuery(tenantId.Value, memberId.Value)));
            })
            .RequireAuthorization(Permissions.TransferAssistanceParticipate)
            .WithSummary("List pending witness invitations for the current member")
            .Produces<IReadOnlyList<TransferAssistanceMyInvitationResponse>>();

        requests.MapGet("/me", async (
                ClaimsPrincipal principal,
                IMediator mediator,
                int page = 1,
                int limit = 20) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();

                return Results.Ok(await mediator.Send(
                    new ListMyTransferAssistanceRequestsQuery(tenantId.Value, memberId.Value, page, limit)));
            })
            .RequireAuthorization(Permissions.TransferAssistanceReadOwn)
            .WithSummary("List the current member's transfer assistance requests")
            .Produces<PagedResult<TransferAssistanceRequestResponse>>();

        requests.MapGet("/beneficiaries", async (
                string query,
                ClaimsPrincipal principal,
                IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();
                if (query.Trim().Length < 3)
                    return Results.BadRequest(new { error = "errors.transfer_assistance.search_too_short" });
                return Results.Ok(await mediator.Send(
                    new SearchTransferAssistanceBeneficiariesQuery(tenantId.Value, memberId.Value, query)));
            })
            .RequireAuthorization(Permissions.TransferAssistanceCreate)
            .WithSummary("Search active, email-verified beneficiary members in the current tenant")
            .Produces<IReadOnlyList<TransferAssistanceBeneficiaryOption>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        requests.MapGet("/witnesses", async (
                string query,
                ClaimsPrincipal principal,
                IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();
                if (query.Trim().Length < 3)
                    return Results.BadRequest(new { error = "errors.transfer_assistance.search_too_short" });
                return Results.Ok(await mediator.Send(
                    new SearchTransferAssistanceWitnessesQuery(tenantId.Value, memberId.Value, query)));
            })
            .RequireAuthorization(Permissions.TransferAssistanceReadOwn)
            .WithSummary("Search verified, opted-in witness members in the current tenant")
            .Produces<IReadOnlyList<TransferAssistanceBeneficiaryOption>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        requests.MapGet("/{id:guid}", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();

                var response = await mediator.Send(
                    new GetMyTransferAssistanceRequestQuery(tenantId.Value, memberId.Value, id));
                return response is null ? Results.NotFound(new { error = "errors.transfer_assistance.not_found" })
                    : Results.Ok(response);
            })
            .RequireAuthorization(Permissions.TransferAssistanceReadOwn)
            .WithSummary("Get one of the current member's requests")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        requests.MapGet("/{id:guid}/offers", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();
                var result = await mediator.Send(new ListMyTransferAssistanceOffersQuery(
                    tenantId.Value, memberId.Value, id));
                return result.IsSuccess
                    ? Results.Ok(result.Data)
                    : Results.NotFound(new { error = result.ErrorCode });
            })
            .RequireAuthorization(Permissions.TransferAssistanceReadOwn)
            .WithSummary("List offers for a request owned by the current member")
            .Produces<IReadOnlyList<TransferAssistanceOfferResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        requests.MapPost("/{id:guid}/offers", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                SubmitTransferAssistanceOfferRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new SubmitTransferAssistanceOfferCommand(
                    tenantId.Value, memberId.Value, userId.Value, id, request));
                return result.IsSuccess
                    ? Results.Created($"/api/v1/transfer-requests/{id}/offers/{result.Data!.Id}", result.Data)
                    : result.ErrorCode == "errors.transfer_assistance.request_not_available"
                        ? Results.Conflict(new { error = result.ErrorCode })
                        : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.TransferAssistanceOffer)
            .WithSummary("Submit an offer for an anonymized matching request")
            .Produces<TransferAssistanceOfferResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{id:guid}/select-offer", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                SelectTransferAssistanceOfferRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new SelectTransferAssistanceOfferCommand(
                    tenantId.Value, memberId.Value, userId.Value, id, request.OfferId));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceReadOwn)
            .WithSummary("Select one helper offer for an owned request")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{id:guid}/witness-invitations", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                InviteTransferAssistanceWitnessRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new InviteTransferAssistanceWitnessCommand(
                    tenantId.Value, memberId.Value, userId.Value, id, request));
                return result.IsSuccess
                    ? Results.Created($"/api/v1/transfer-assistance/witness-invitations/{result.Data!.Id}", result.Data)
                    : Results.Conflict(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.TransferAssistanceReadOwn)
            .WithSummary("Invite a verified, independent witness to an owned request")
            .Produces<TransferAssistanceWitnessInvitationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/witness-invitations/{invitationId:guid}/respond", async (
                Guid invitationId,
                ClaimsPrincipal principal,
                IMediator mediator,
                RespondTransferAssistanceWitnessInvitationRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new RespondTransferAssistanceWitnessInvitationCommand(
                    tenantId.Value, memberId.Value, userId.Value, invitationId, request));
                return result.IsSuccess
                    ? Results.Ok(result.Data)
                    : Results.Conflict(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.TransferAssistanceParticipate)
            .WithSummary("Accept or decline a witness invitation and declare conflicts of interest")
            .Produces<TransferAssistanceWitnessInvitationResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{id:guid}/meeting", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                ScheduleTransferAssistanceMeetingRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new ScheduleTransferAssistanceMeetingCommand(
                    tenantId.Value, memberId.Value, userId.Value, id, request));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceParticipate)
            .WithSummary("Schedule the non-financial handover appointment")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{id:guid}/handover-confirmation", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                TransferAssistanceHandoverConfirmationRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new ConfirmTransferAssistanceHandoverCommand(
                    tenantId.Value, memberId.Value, userId.Value, id, request));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceParticipate)
            .WithSummary("Record a participant's declaration of presence and handover")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{id:guid}/witness-attestation", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                AttestTransferAssistanceWitnessRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new AttestTransferAssistanceWitnessCommand(
                    tenantId.Value, memberId.Value, userId.Value, id, request.Attested));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceParticipate)
            .WithSummary("Record an independent witness's attestation")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{id:guid}/transfer-evidence", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                SubmitTransferAssistanceEvidenceRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new SubmitTransferAssistanceEvidenceCommand(
                    tenantId.Value, memberId.Value, userId.Value, id, request));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceParticipate)
            .WithSummary("Record a provider-issued reference as a declaration; does not execute a transfer")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{id:guid}/receipt-confirmation", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                ConfirmTransferAssistanceReceiptRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new ConfirmTransferAssistanceReceiptCommand(
                    tenantId.Value, memberId.Value, userId.Value, id, request));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceConfirmReceipt)
            .WithSummary("Let the authenticated beneficiary confirm full, partial, or missing receipt")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{id:guid}/disputes", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                OpenTransferAssistanceDisputeRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new OpenTransferAssistanceDisputeCommand(
                    tenantId.Value, memberId.Value, userId.Value, id, request.Reason));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceOpenDispute)
            .WithSummary("Open an audited dispute as a request participant")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapGet("/{id:guid}/events", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();
                var events = await mediator.Send(new TransferAssistanceEventQuery(
                    tenantId.Value, memberId.Value, id, false));
                return events is null
                    ? Results.NotFound(new { error = "errors.transfer_assistance.not_found" })
                    : Results.Ok(events);
            })
            .RequireAuthorization(Permissions.TransferAssistanceReadOwn)
            .WithSummary("Read the audit timeline for a request participant")
            .Produces<IReadOnlyList<TransferAssistanceEventResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        requests.MapPost("/", async (
                ClaimsPrincipal principal,
                IMediator mediator,
                CreateTransferAssistanceRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();

                var result = await mediator.Send(
                    new CreateTransferAssistanceRequestCommand(tenantId.Value, memberId.Value, request));
                return result.IsSuccess
                    ? Results.Created($"/api/v1/transfer-requests/{result.Data!.Id}", result.Data)
                    : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.TransferAssistanceCreate)
            .WithSummary("Create a transfer assistance draft without moving funds")
            .Produces<TransferAssistanceRequestResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        requests.MapPut("/{id:guid}", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                CreateTransferAssistanceRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new UpdateTransferAssistanceDraftCommand(
                    tenantId.Value, memberId.Value, userId.Value, id, request));
                return result.IsSuccess
                    ? Results.Ok(result.Data)
                    : result.ErrorCode == "errors.transfer_assistance.invalid_transition"
                        ? Results.Conflict(new { error = result.ErrorCode })
                        : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.TransferAssistanceCreate)
            .WithSummary("Update an owned draft before submission")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{id:guid}/submit", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null) return Results.Forbid();
                if (userId is null) return Results.Forbid();

                var result = await mediator.Send(
                    new SubmitTransferAssistanceRequestCommand(tenantId.Value, memberId.Value, userId.Value, id));
                return result.IsSuccess
                    ? Results.Ok(result.Data)
                    : result.ErrorCode == "errors.transfer_assistance.not_found"
                        ? Results.NotFound(new { error = result.ErrorCode })
                        : Results.Conflict(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.TransferAssistanceCreate)
            .WithSummary("Submit a draft for manual review")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{id:guid}/cancel", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                var memberId = GetMemberId(principal);
                var userId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (memberId is null || userId is null) return Results.Forbid();
                var result = await mediator.Send(new CancelTransferAssistanceRequestCommand(
                    tenantId.Value, memberId.Value, userId.Value, id));
                return result.IsSuccess
                    ? Results.Ok(result.Data)
                    : result.ErrorCode == "errors.transfer_assistance.not_found"
                        ? Results.NotFound(new { error = result.ErrorCode })
                        : Results.Conflict(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .RequireAuthorization(Permissions.TransferAssistanceCreate)
            .WithSummary("Cancel an owned request before the handover process")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        var reviews = app.MapGroup("/api/v1/admin/transfer-assistance-requests")
            .WithTags("Transfer Assistance Review")
            .RequireAuthorization(Permissions.TransferAssistanceReview);

        reviews.MapGet("/review-queue", async (
                ClaimsPrincipal principal,
                IMediator mediator,
                int page = 1,
                int limit = 20) =>
            {
                var tenantId = GetTenantId(principal);
                if (tenantId is null) return Results.Unauthorized();
                return Results.Ok(await mediator.Send(
                    new ListTransferAssistanceReviewQueueQuery(tenantId.Value, page, limit)));
            })
            .WithSummary("List submitted requests for manual review in the current tenant")
            .Produces<PagedResult<TransferAssistanceRequestResponse>>();

        reviews.MapGet("/cases/{status}", async (
                string status,
                ClaimsPrincipal principal,
                IMediator mediator,
                int page = 1,
                int limit = 20) =>
            {
                var tenantId = GetTenantId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (status is not (TransferAssistanceStatuses.Disputed or TransferAssistanceStatuses.Suspended))
                    return Results.BadRequest(new { error = "errors.transfer_assistance.invalid_case_status" });
                return Results.Ok(await mediator.Send(
                    new ListTransferAssistanceReviewCasesQuery(tenantId.Value, status, page, limit)));
            })
            .WithSummary("List suspended requests or open disputes for compliance review")
            .Produces<PagedResult<TransferAssistanceReviewCaseResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        reviews.MapGet("/{id:guid}/events", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator) =>
            {
                var tenantId = GetTenantId(principal);
                if (tenantId is null) return Results.Unauthorized();
                var events = await mediator.Send(new TransferAssistanceEventQuery(
                    tenantId.Value, Guid.Empty, id, true));
                return events is null
                    ? Results.NotFound(new { error = "errors.transfer_assistance.not_found" })
                    : Results.Ok(events);
            })
            .WithSummary("Read a request audit timeline as an authorized reviewer")
            .Produces<IReadOnlyList<TransferAssistanceEventResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        reviews.MapPost("/{id:guid}/review", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                ReviewTransferAssistanceRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var reviewerId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (reviewerId is null) return Results.Forbid();

                var result = await mediator.Send(new ReviewTransferAssistanceRequestCommand(
                    tenantId.Value, reviewerId.Value, id, request));
                return result.IsSuccess
                    ? Results.Ok(result.Data)
                    : result.ErrorCode == "errors.transfer_assistance.not_found"
                        ? Results.NotFound(new { error = result.ErrorCode })
                        : result.ErrorCode == "errors.validation.invalid"
                            ? Results.BadRequest(new { error = result.ErrorCode })
                            : Results.Conflict(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .WithSummary("Approve, reject, or suspend a submitted request")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        reviews.MapPost("/{id:guid}/resolve-dispute", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                ResolveTransferAssistanceDisputeRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var reviewerId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (reviewerId is null) return Results.Forbid();
                var result = await mediator.Send(new ResolveTransferAssistanceDisputeCommand(
                    tenantId.Value, reviewerId.Value, id, request));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceResolveDispute)
            .WithSummary("Resolve a dispute by requiring beneficiary re-confirmation or closing the request")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        reviews.MapPost("/{id:guid}/suspend", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                OpenTransferAssistanceDisputeRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var reviewerId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (reviewerId is null) return Results.Forbid();
                var result = await mediator.Send(new SuspendTransferAssistanceRequestCommand(
                    tenantId.Value, reviewerId.Value, id, request.Reason));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceSuspend)
            .WithSummary("Suspend an active request for compliance review")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        reviews.MapPost("/{id:guid}/resume", async (
                Guid id,
                ClaimsPrincipal principal,
                IMediator mediator,
                OpenTransferAssistanceDisputeRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var reviewerId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (reviewerId is null) return Results.Forbid();
                var result = await mediator.Send(new ResumeTransferAssistanceRequestCommand(
                    tenantId.Value, reviewerId.Value, id, request.Reason));
                return WorkflowResult(result);
            })
            .RequireAuthorization(Permissions.TransferAssistanceSuspend)
            .WithSummary("Resume a suspended request with a mandatory reason")
            .Produces<TransferAssistanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        var profileReviews = app.MapGroup("/api/v1/admin/transfer-assistance-profiles")
            .WithTags("Transfer Assistance Profile Review")
            .RequireAuthorization(Permissions.TransferAssistanceVerifyUsers);

        profileReviews.MapGet("/review-queue", async (
                ClaimsPrincipal principal,
                IMediator mediator,
                int page = 1,
                int limit = 20) =>
            {
                var tenantId = GetTenantId(principal);
                if (tenantId is null) return Results.Unauthorized();
                return Results.Ok(await mediator.Send(
                    new ListTransferAssistanceProfileReviewQueueQuery(tenantId.Value, page, limit)));
            })
            .WithSummary("List participant profiles awaiting manual verification")
            .Produces<PagedResult<TransferAssistanceProfileReviewItem>>();

        profileReviews.MapPost("/{memberId:guid}/review", async (
                Guid memberId,
                ClaimsPrincipal principal,
                IMediator mediator,
                ReviewTransferAssistanceProfileRequest request) =>
            {
                var tenantId = GetTenantId(principal);
                var reviewerId = GetUserId(principal);
                if (tenantId is null) return Results.Unauthorized();
                if (reviewerId is null) return Results.Forbid();
                var result = await mediator.Send(new ReviewTransferAssistanceProfileCommand(
                    tenantId.Value, reviewerId.Value, memberId, request));
                return result.IsSuccess
                    ? Results.Ok(result.Data)
                    : result.ErrorCode == "errors.transfer_assistance.profile_not_found"
                        ? Results.NotFound(new { error = result.ErrorCode })
                        : result.ErrorCode == "errors.transfer_assistance.invalid_transition"
                            ? Results.Conflict(new { error = result.ErrorCode })
                            : Results.BadRequest(new { error = result.ErrorCode ?? result.ErrorMessage });
            })
            .WithSummary("Manually verify or reject one opted-in participant role")
            .Produces<TransferAssistanceProfileResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("tenant_id"), out var id) ? id : null;

    private static Guid? GetMemberId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("entity_id"), out var id) ? id : null;

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static IReadOnlyCollection<string> GetPermissions(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirstValue("permissions");
        if (string.IsNullOrWhiteSpace(claim)) return [];
        try { return JsonSerializer.Deserialize<string[]>(claim) ?? []; }
        catch (JsonException) { return []; }
    }

    private static IResult WorkflowResult<T>(ServiceResult<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ErrorCode == "errors.transfer_assistance.not_found" ||
              result.ErrorCode == "errors.transfer_assistance.dispute_not_found"
                ? Results.NotFound(new { error = result.ErrorCode })
                : result.ErrorCode == "errors.validation.invalid"
                    ? Results.BadRequest(new { error = result.ErrorCode })
                    : Results.Conflict(new { error = result.ErrorCode ?? result.ErrorMessage });
}
