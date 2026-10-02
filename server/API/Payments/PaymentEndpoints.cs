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

        group.MapGet("/{id:guid}/receipt", async (
            Guid id,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();

            var payment = await db.Payments
                .Include(p => p.Member).ThenInclude(m => m.User)
                .Include(p => p.Charge).ThenInclude(c => c.ContributionType)
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);
            if (payment is null) return Results.NotFound(new { error = "Payment not found." });

            var tenant = await db.Tenants.FindAsync(tenantId.Value);
            var tenantName = tenant?.Name ?? "Association";
            var currency = payment.Currency;
            var symbol = tenant?.CurrencySymbol ?? "€";

            var confirmedRow = payment.ConfirmedAt.HasValue
                ? $"<tr><td>Confirmé le</td><td>{payment.ConfirmedAt.Value:dd/MM/yyyy HH:mm}</td></tr>"
                : "";
            var notesRow = !string.IsNullOrEmpty(payment.Notes)
                ? $"<tr><td>Notes</td><td>{payment.Notes}</td></tr>"
                : "";
            var css = """
body{font-family:Arial,sans-serif;max-width:600px;margin:40px auto;color:#1a1a1a}
.header{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:32px;border-bottom:2px solid #10b981;padding-bottom:16px}
.org{font-size:20px;font-weight:bold;color:#10b981}
.receipt-title{font-size:14px;color:#6b7280}
.receipt-number{font-size:22px;font-weight:bold;margin-top:4px}
table{width:100%;border-collapse:collapse;margin:16px 0}
td{padding:8px 0;vertical-align:top}
td:last-child{text-align:right;font-weight:500}
.section-title{font-size:11px;text-transform:uppercase;letter-spacing:.05em;color:#9ca3af;margin:24px 0 8px}
.amount-row{font-size:20px;font-weight:bold;color:#10b981;border-top:2px solid #e5e7eb;padding-top:12px;margin-top:8px}
.status-badge{display:inline-block;padding:2px 10px;border-radius:9999px;font-size:12px;font-weight:600}
.status-confirmed{background:#d1fae5;color:#065f46}
.status-pending{background:#fef3c7;color:#92400e}
.footer{margin-top:40px;font-size:11px;color:#9ca3af;text-align:center;border-top:1px solid #e5e7eb;padding-top:16px}
@media print{.no-print{display:none}}
""";
            var html = $"""
<!DOCTYPE html>
<html lang="fr">
<head>
  <meta charset="UTF-8" />
  <title>Reçu {payment.ReceiptNumber}</title>
  <style>{css}</style>
</head>
<body>
  <div class="header">
    <div>
      <div class="org">{tenantName}</div>
      <div style="color:#6b7280;font-size:13px;margin-top:4px;">Reçu de paiement</div>
    </div>
    <div style="text-align:right">
      <div class="receipt-title">N° de reçu</div>
      <div class="receipt-number">{payment.ReceiptNumber}</div>
      <div style="margin-top:6px"><span class="status-badge status-{payment.Status}">{payment.Status}</span></div>
    </div>
  </div>
  <div class="section-title">Membre</div>
  <table>
    <tr><td>Nom</td><td>{payment.Member.User.FirstName} {payment.Member.User.LastName}</td></tr>
    <tr><td>N° adhérent</td><td>{payment.Member.MembershipNumber}</td></tr>
  </table>
  <div class="section-title">Paiement</div>
  <table>
    <tr><td>Type de contribution</td><td>{payment.Charge.ContributionType.Name}</td></tr>
    <tr><td>Date de paiement</td><td>{payment.PaymentDate:dd/MM/yyyy}</td></tr>
    <tr><td>Méthode</td><td>{payment.PaymentGateway ?? "—"}</td></tr>
    {confirmedRow}{notesRow}
  </table>
  <table>
    <tr class="amount-row"><td>Montant payé</td><td>{symbol}{payment.Amount:N2} {currency}</td></tr>
  </table>
  <button class="no-print" onclick="window.print()" style="margin-top:24px;padding:10px 24px;background:#10b981;color:white;border:none;border-radius:8px;cursor:pointer;font-size:14px;">
    Imprimer / Enregistrer en PDF
  </button>
  <div class="footer">
    Ce reçu a été généré automatiquement par {tenantName} le {DateTime.UtcNow:dd/MM/yyyy} UTC.
  </div>
</body>
</html>
""";

            return Results.Content(html, "text/html");
        })
        .RequireAuthorization(Permissions.PaymentsRead);

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
            var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "attachments", "receipts");
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, uniqueName);
            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream);

            var baseUrl = $"{request.Scheme}://{request.Host}";
            payment.ReceiptFileUrl = $"{baseUrl}/attachments/receipts/{uniqueName}";
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
        if (entityType is "board_member" or "super_admin") return requestedMemberId;
        return principal.FindFirstValue("entity_id");
    }

    private static Guid? ParseEnforcedMemberId(ClaimsPrincipal principal)
    {
        var raw = EnforceOwnMemberId(principal, null);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
