using System.Security.Claims;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Payments.Commands;
using server.Application.Payments.DTOs;
using server.Application.Payments.Queries;
using server.Infrastructure.Persistence;

namespace server.API.Payments;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payments").WithTags("Payments").RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20,
            string? member_id = null, string? status = null,
            string? from = null, string? to = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var enforcedMemberId = EnforceOwnMemberId(principal, member_id);
            var result = await mediator.Send(new ListPaymentsQuery(tenantId.Value, page, limit, enforcedMemberId, status, from, to));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.PaymentsRead);

        group.MapGet("/stats", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var enforcedMemberId = ParseEnforcedMemberId(principal);
            var result = await mediator.Send(new GetPaymentStatsQuery(tenantId.Value, enforcedMemberId));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.PaymentsRead);

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new GetPaymentByIdQuery(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.PaymentsRead);

        group.MapPost("/", async (ClaimsPrincipal principal, IMediator mediator, RecordPaymentRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new RecordPaymentCommand(tenantId.Value, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.PaymentsCreate);

        group.MapPost("/{id:guid}/confirm", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var userId = GetUserId(principal);
            var result = await mediator.Send(new ConfirmPaymentCommand(tenantId.Value, id, userId ?? Guid.Empty));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.PaymentsValidate);

        group.MapPost("/{id:guid}/reverse", async (Guid id, ClaimsPrincipal principal, IMediator mediator, ReversePaymentRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var userId = GetUserId(principal);
            var result = await mediator.Send(new ReversePaymentCommand(tenantId.Value, id, userId ?? Guid.Empty, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.PaymentsRefund);

        group.MapPost("/{id:guid}/receipt", async (
            Guid id,
            IFormFile file,
            ClaimsPrincipal principal,
            IWebHostEnvironment env,
            AppDbContext db,
            HttpRequest request) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);
            if (payment is null) return Results.NotFound(new { error = "Payment not found." });

            if (file.Length == 0) return Results.BadRequest(new { error = "No file provided." });
            if (file.Length > 10 * 1024 * 1024) return Results.BadRequest(new { error = "File exceeds 10 MB." });

            var ext = Path.GetExtension(file.FileName);
            var uniqueName = $"receipt_{id}{ext}";
            var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "receipts");
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, uniqueName);
            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream);

            var baseUrl = $"{request.Scheme}://{request.Host}";
            payment.ReceiptFileUrl = $"{baseUrl}/uploads/receipts/{uniqueName}";
            payment.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { receipt_file_url = payment.ReceiptFileUrl });
        })
        .RequireAuthorization(Permissions.PaymentsReceiptPrint)
        .DisableAntiforgery();
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("tenant_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static string? EnforceOwnMemberId(ClaimsPrincipal principal, string? requestedMemberId)
    {
        var entityType = principal.FindFirstValue("entity_type");
        if (entityType == "board_member") return requestedMemberId;
        return principal.FindFirstValue("entity_id");
    }

    private static Guid? ParseEnforcedMemberId(ClaimsPrincipal principal)
    {
        var raw = EnforceOwnMemberId(principal, null);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
