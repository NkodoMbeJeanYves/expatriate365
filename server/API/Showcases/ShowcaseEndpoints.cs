using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.API.Showcases;

public static partial class ShowcaseEndpoints
{
    public static void MapShowcaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/showcases").WithTags("Community showcases");

        group.MapGet("/{tenantSlug}", async (
            string tenantSlug,
            AppDbContext db,
            int page = 1,
            int limit = 20,
            CancellationToken ct = default) =>
        {
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == tenantSlug && t.IsActive, ct);
            if (tenant is null)
                return Results.NotFound(new { error = "Community not found." });

            var query = PublicShowcases(db, tenant.Id);
            var total = await query.CountAsync(ct);
            var effectivePage = Math.Max(page, 1);
            var effectiveLimit = Math.Clamp(limit, 1, 100);
            var data = await query.Include(s => s.Tenant).Include(s => s.Offers)
                .Include(s => s.Member).ThenInclude(m => m.User)
                .OrderByDescending(s => s.PublishedAt)
                .Skip((effectivePage - 1) * effectiveLimit)
                .Take(effectiveLimit)
                .ToListAsync(ct);

            return Results.Ok(PagedResult<ShowcaseResponse>.Create(
                data.Select(ToResponse).ToList(), effectivePage, effectiveLimit, total));
        })
        .WithSummary("List published showcases for a community.")
        .Produces<PagedResult<ShowcaseResponse>>();

        group.MapGet("/{tenantSlug}/{showcaseSlug}", async (
            string tenantSlug,
            string showcaseSlug,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var showcase = await PublicShowcases(db)
                .Where(s => s.Tenant.Slug == tenantSlug && s.Slug == showcaseSlug)
                .Include(s => s.Tenant)
                .Include(s => s.Offers)
                .Include(s => s.Member).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(ct);
            return showcase is null
                ? Results.NotFound(new { error = "Showcase not found." })
                : Results.Ok(ToResponse(showcase));
        })
        .WithSummary("Get a published showcase.")
        .Produces<ShowcaseResponse>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{tenantSlug}/{showcaseSlug}/contact", async (
            string tenantSlug,
            string showcaseSlug,
            ShowcaseContactRequest request,
            AppDbContext db,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            if (!string.IsNullOrWhiteSpace(request.Website))
                return Results.Accepted();

            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 160 ||
                string.IsNullOrWhiteSpace(request.Email) ||
                !new EmailAddressAttribute().IsValid(request.Email) || request.Email.Length > 254 ||
                string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 3000)
                return Results.BadRequest(new { error = "Please provide a valid name, email, and message." });

            var owner = await PublicShowcases(db)
                .Where(s => s.Tenant.Slug == tenantSlug && s.Slug == showcaseSlug)
                .Select(s => new
                {
                    s.Id,
                    s.TenantId,
                    OwnerId = s.Member.UserId,
                    OwnerName = s.Member.User.FirstName + " " + s.Member.User.LastName,
                    s.Member.User.ContactEmail,
                    s.Member.User.PreferredLanguage,
                    s.ContactEnabled,
                    s.Title,
                })
                .FirstOrDefaultAsync(ct);
            if (owner is null)
                return Results.NotFound(new { error = "Showcase not found." });
            if (!owner.ContactEnabled || string.IsNullOrWhiteSpace(owner.ContactEmail))
                return Results.Conflict(new { error = "The showcase owner has not enabled email contact." });

            var visitorEmail = request.Email.Trim();
            var emailSeparator = visitorEmail.LastIndexOf('@');
            var replyEmail = $"{Uri.EscapeDataString(visitorEmail[..emailSeparator])}@" +
                Uri.EscapeDataString(visitorEmail[(emailSeparator + 1)..]);
            var isEnglish = string.Equals(owner.PreferredLanguage, "en", StringComparison.OrdinalIgnoreCase);
            var subject = isEnglish ? "New message about a community showcase" : "Nouveau message au sujet d'une vitrine communautaire";
            var html = $"""
                <p>{(isEnglish ? "A visitor contacted you about your community showcase" : "Un visiteur vous a écrit au sujet de votre vitrine communautaire")} <strong>{HtmlEncoder.Default.Encode(owner.Title)}</strong>.</p>
                <p><strong>{(isEnglish ? "Name" : "Nom")}:</strong> {HtmlEncoder.Default.Encode(request.Name.Trim())}<br>
                <strong>{(isEnglish ? "Email" : "E-mail")}:</strong> <a href="mailto:{replyEmail}">{HtmlEncoder.Default.Encode(visitorEmail)}</a></p>
                <p><strong>{(isEnglish ? "Message" : "Message")}:</strong></p><p>{HtmlEncoder.Default.Encode(request.Message.Trim()).Replace("\n", "<br>")}</p>
                <p>{(isEnglish ? "Use the visitor's email address above to reply." : "Utilisez l'adresse e-mail ci-dessus pour répondre au visiteur.")}</p>
                """;
            var queued = await EmailOutbox.EnqueueAsync(
                db,
                owner.TenantId,
                owner.OwnerId,
                owner.ContactEmail,
                owner.OwnerName.Trim(),
                owner.PreferredLanguage,
                EmailOutboxTemplates.PreRenderedHtml,
                new { subject, html },
                $"showcase-contact:{owner.Id:N}:{Guid.NewGuid():N}",
                ct);
            if (!queued)
                return Results.Conflict(new { error = "The message could not be queued." });

            await db.SaveChangesAsync(ct);
            loggerFactory.CreateLogger("ShowcaseContact").LogInformation(
                "Public contact message queued for showcase {ShowcaseId}", owner.Id);
            return Results.Accepted();
        })
        .RequireRateLimiting("public-showcase")
        .WithSummary("Send a private message to a showcase owner.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{tenantSlug}/{showcaseSlug}/reports", async (
            string tenantSlug,
            string showcaseSlug,
            ShowcaseReportRequest request,
            AppDbContext db,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            if (!string.IsNullOrWhiteSpace(request.Website))
                return Results.Accepted();
            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
                return Results.BadRequest(new { error = "A report reason is required." });

            var showcase = await PublicShowcases(db)
                .Where(s => s.Tenant.Slug == tenantSlug && s.Slug == showcaseSlug)
                .Select(s => new { s.Id, s.TenantId })
                .FirstOrDefaultAsync(ct);
            if (showcase is null)
                return Results.NotFound(new { error = "Showcase not found." });

            db.CommunityShowcaseReports.Add(new CommunityShowcaseReport
            {
                Id = Guid.NewGuid(),
                TenantId = showcase.TenantId,
                ShowcaseId = showcase.Id,
                Reason = request.Reason.Trim(),
            });
            await db.SaveChangesAsync(ct);
            loggerFactory.CreateLogger("ShowcaseModeration").LogInformation(
                "Public report received for showcase {ShowcaseId}", showcase.Id);
            return Results.Accepted();
        })
        .RequireRateLimiting("public-showcase")
        .WithSummary("Report a public showcase.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        var manage = app.MapGroup("/api/v1/my/showcases")
            .WithTags("Community showcases")
            .RequireAuthorization();

        manage.MapGet("/me", async (
            ClaimsPrincipal principal,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(principal);
            var userId = GetUserId(principal);
            if (tenantId is null || userId is null)
                return Results.Unauthorized();
            var memberId = await FindActiveMemberIdAsync(db, tenantId.Value, userId.Value, ct);
            if (memberId is null)
                return Results.Forbid();
            var showcase = await db.CommunityShowcases.AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.MemberId == memberId)
                .Include(s => s.Tenant)
                .Include(s => s.Offers)
                .Include(s => s.Member).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(ct);
            return Results.Ok(showcase is null ? null : ToResponse(showcase));
        })
        .WithSummary("Get the current member's showcase.")
        .Produces<ShowcaseResponse?>();

        manage.MapPut("/me", async (
            SaveShowcaseRequest request,
            ClaimsPrincipal principal,
            AppDbContext db,
            HttpRequest httpRequest,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(principal);
            var userId = GetUserId(principal);
            if (tenantId is null || userId is null)
                return Results.Unauthorized();
            var memberId = await FindActiveMemberIdAsync(db, tenantId.Value, userId.Value, ct);
            if (memberId is null)
                return Results.Forbid();
            var validationError = ValidateSaveRequest(request, httpRequest, configuration);
            if (validationError is not null)
                return Results.BadRequest(new { error = validationError });

            var showcase = await db.CommunityShowcases
                .Include(s => s.Offers)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.MemberId == memberId, ct);
            var created = showcase is null;
            if (showcase is null)
            {
                showcase = new CommunityShowcase
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    MemberId = memberId.Value,
                    Slug = await CreateSlugAsync(db, tenantId.Value, request.Title, ct),
                };
                db.CommunityShowcases.Add(showcase);
            }

            showcase.DisplayName = request.DisplayName.Trim();
            showcase.Title = request.Title.Trim();
            showcase.Description = request.Description.Trim();
            showcase.Category = request.Category.Trim();
            showcase.ServiceArea = CleanOptional(request.ServiceArea);
            showcase.ContactPhone = CleanOptional(request.ContactPhone);
            showcase.ContactLink = CleanOptional(request.ContactLink);
            showcase.ContactEnabled = request.ContactEnabled;
            showcase.PhotoUrl = CleanOptional(request.PhotoUrl);

            var existingOffers = showcase.Offers.OrderBy(offer => offer.Position).ToList();
            for (var position = 0; position < request.Offers.Count; position++)
            {
                var requestOffer = request.Offers[position];
                if (position < existingOffers.Count)
                {
                    var existingOffer = existingOffers[position];
                    existingOffer.Name = requestOffer.Name.Trim();
                    existingOffer.Description = requestOffer.Description.Trim();
                    existingOffer.Price = requestOffer.Price;
                    existingOffer.PriceOnRequest = requestOffer.PriceOnRequest;
                    existingOffer.Details = CleanOptional(requestOffer.Details);
                    existingOffer.Position = position;
                    continue;
                }

                showcase.Offers.Add(new CommunityShowcaseOffer
                {
                    Id = Guid.NewGuid(),
                    ShowcaseId = showcase.Id,
                    Name = requestOffer.Name.Trim(),
                    Description = requestOffer.Description.Trim(),
                    Price = requestOffer.Price,
                    PriceOnRequest = requestOffer.PriceOnRequest,
                    Details = CleanOptional(requestOffer.Details),
                    Position = position,
                });
            }

            if (request.Offers.Count < existingOffers.Count)
                db.CommunityShowcaseOffers.RemoveRange(existingOffers.Skip(request.Offers.Count));

            showcase.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            loggerFactory.CreateLogger("ShowcaseManagement").LogInformation(
                "Showcase {ShowcaseId} saved by member {MemberId}", showcase.Id, memberId);
            return created
                ? Results.Created($"/api/v1/my/showcases/me", await LoadResponseAsync(db, showcase.Id, ct))
                : Results.Ok(await LoadResponseAsync(db, showcase.Id, ct));
        })
        .WithSummary("Create or update the current member's showcase.")
        .Produces<ShowcaseResponse>()
        .Produces(StatusCodes.Status400BadRequest);

        manage.MapPost("/me/publish", async (
            ClaimsPrincipal principal,
            AppDbContext db,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(principal);
            var userId = GetUserId(principal);
            if (tenantId is null || userId is null)
                return Results.Unauthorized();
            var memberId = await FindActiveMemberIdAsync(db, tenantId.Value, userId.Value, ct);
            if (memberId is null)
                return Results.Forbid();
            var showcase = await db.CommunityShowcases.Include(s => s.Offers)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.MemberId == memberId, ct);
            if (showcase is null)
                return Results.NotFound(new { error = "Create your showcase before publishing it." });
            if (showcase.Status == "suspended")
                return Results.Conflict(new { error = "A suspended showcase cannot be republished. Contact a moderator." });
            if (string.IsNullOrWhiteSpace(showcase.Title) ||
                string.IsNullOrWhiteSpace(showcase.Description) ||
                showcase.Offers.Count == 0)
                return Results.BadRequest(new { error = "Add a title, description, and at least one offer before publishing." });

            showcase.Status = "published";
            showcase.PublishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            loggerFactory.CreateLogger("ShowcaseManagement").LogInformation(
                "Showcase {ShowcaseId} published by member {MemberId}", showcase.Id, memberId);
            return Results.Ok(await LoadResponseAsync(db, showcase.Id, ct));
        })
        .WithSummary("Publish the current member's showcase.")
        .Produces<ShowcaseResponse>()
        .Produces(StatusCodes.Status404NotFound);

        manage.MapPost("/me/unpublish", async (
            ClaimsPrincipal principal,
            AppDbContext db,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(principal);
            var userId = GetUserId(principal);
            if (tenantId is null || userId is null)
                return Results.Unauthorized();
            var memberId = await FindActiveMemberIdAsync(db, tenantId.Value, userId.Value, ct);
            if (memberId is null)
                return Results.Forbid();
            var showcase = await db.CommunityShowcases
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.MemberId == memberId, ct);
            if (showcase is null)
                return Results.NotFound(new { error = "Showcase not found." });
            if (showcase.Status == "suspended")
                return Results.Conflict(new { error = "A suspended showcase cannot be unpublished or restored by its owner." });
            showcase.Status = "draft";
            await db.SaveChangesAsync(ct);
            loggerFactory.CreateLogger("ShowcaseManagement").LogInformation(
                "Showcase {ShowcaseId} unpublished by member {MemberId}", showcase.Id, memberId);
            return Results.Ok(await LoadResponseAsync(db, showcase.Id, ct));
        })
        .WithSummary("Unpublish the current member's showcase.")
        .Produces<ShowcaseResponse>()
        .Produces(StatusCodes.Status404NotFound);

        var moderation = app.MapGroup("/api/v1/moderation/showcases")
            .WithTags("Community showcases")
            .RequireAuthorization(Permissions.CommunityModerate);
        moderation.MapGet("/reports", async (
            ClaimsPrincipal principal,
            AppDbContext db,
            int page = 1,
            int limit = 20,
            CancellationToken ct = default) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null)
                return Results.Unauthorized();
            var query = db.CommunityShowcaseReports.AsNoTracking()
                .Where(r => r.TenantId == tenantId && r.Status == "pending");
            var total = await query.CountAsync(ct);
            var effectivePage = Math.Max(page, 1);
            var effectiveLimit = Math.Clamp(limit, 1, 100);
            var reportRows = await query.OrderBy(r => r.CreatedAt)
                .Skip((effectivePage - 1) * effectiveLimit)
                .Take(effectiveLimit)
                .Select(r => new
                {
                    r.Id,
                    r.ShowcaseId,
                    ShowcaseTitle = r.Showcase.Title,
                    ShowcaseSlug = r.Showcase.Slug,
                    r.Reason,
                    r.Status,
                    r.ResolutionNote,
                    r.CreatedAt,
                })
                .ToListAsync(ct);
            var data = reportRows.Select(r => new ShowcaseReportResponse(
                r.Id, r.ShowcaseId, r.ShowcaseTitle, r.ShowcaseSlug, r.Reason, r.Status,
                r.ResolutionNote, new DateTimeOffset(r.CreatedAt, TimeSpan.Zero))).ToList();
            return Results.Ok(PagedResult<ShowcaseReportResponse>.Create(
                data, effectivePage, effectiveLimit, total));
        })
        .WithSummary("List pending showcase reports for moderation.")
        .Produces<PagedResult<ShowcaseReportResponse>>();

        moderation.MapPost("/reports/{reportId:guid}/resolve", async (
            Guid reportId,
            ResolveShowcaseReportRequest request,
            ClaimsPrincipal principal,
            AppDbContext db,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(principal);
            var userId = GetUserId(principal);
            if (tenantId is null || userId is null)
                return Results.Unauthorized();
            if (request.Action is not ("hide" or "dismiss") ||
                (request.Note?.Length ?? 0) > 1000)
                return Results.BadRequest(new { error = "Action must be 'hide' or 'dismiss' and the note must not exceed 1000 characters." });

            var report = await db.CommunityShowcaseReports
                .Include(r => r.Showcase)
                .FirstOrDefaultAsync(r => r.Id == reportId && r.TenantId == tenantId, ct);
            if (report is null)
                return Results.NotFound(new { error = "Report not found." });
            if (report.Status != "pending")
                return Results.Conflict(new { error = "This report has already been resolved." });

            if (request.Action == "hide")
                report.Showcase.Status = "suspended";
            report.Status = request.Action == "hide" ? "resolved_hidden" : "resolved_dismissed";
            report.HandledByUserId = userId;
            report.ResolutionNote = CleanOptional(request.Note);
            report.HandledAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            loggerFactory.CreateLogger("ShowcaseModeration").LogInformation(
                "Showcase report {ReportId} resolved with action {Action} by user {UserId}",
                report.Id, request.Action, userId);
            return Results.Ok(new { report_id = report.Id, status = report.Status });
        })
        .WithSummary("Hide a reported showcase or dismiss its report.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);
    }

    private static IQueryable<CommunityShowcase> PublicShowcases(AppDbContext db, Guid? tenantId = null) =>
        db.CommunityShowcases.AsNoTracking()
            .Where(s => s.Status == "published" &&
                s.Tenant.IsActive &&
                s.Member.IsActive &&
                s.Member.Status == "active" &&
                (tenantId == null || s.TenantId == tenantId));

    private static ShowcaseResponse ToResponse(CommunityShowcase s) => new(
        s.Id,
        s.Slug,
        s.DisplayName,
        s.Title,
        s.Description,
        s.Category,
        s.ServiceArea,
        s.ContactPhone,
        s.ContactLink,
        s.ContactEnabled,
        s.PhotoUrl,
        s.Status,
        s.Tenant.Name,
        s.Tenant.Slug,
        s.Tenant.BaseCurrency,
        s.Tenant.CurrencySymbol,
        s.ContactEnabled && !string.IsNullOrWhiteSpace(s.Member.User.ContactEmail),
        new DateTimeOffset(s.CreatedAt, TimeSpan.Zero),
        s.UpdatedAt is null ? null : new DateTimeOffset(s.UpdatedAt.Value, TimeSpan.Zero),
        s.Offers.OrderBy(o => o.Position)
            .Select(o => new ShowcaseOfferResponse(
                o.Id, o.Name, o.Description, o.Price, o.PriceOnRequest, o.Details, o.Position))
            .ToArray());

    private static async Task<Guid?> FindActiveMemberIdAsync(
        AppDbContext db, Guid tenantId, Guid userId, CancellationToken ct) =>
        await db.Members.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.UserId == userId &&
                m.IsActive && m.Status == "active")
            .Select(m => (Guid?)m.Id)
            .FirstOrDefaultAsync(ct);

    private static async Task<ShowcaseResponse?> LoadResponseAsync(
        AppDbContext db, Guid id, CancellationToken ct)
    {
        var showcase = await db.CommunityShowcases.AsNoTracking()
            .Where(s => s.Id == id)
            .Include(s => s.Tenant)
            .Include(s => s.Offers)
            .Include(s => s.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(ct);
        return showcase is null ? null : ToResponse(showcase);
    }

    private static string? ValidateSaveRequest(
        SaveShowcaseRequest request, HttpRequest httpRequest, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 160 ||
            string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200 ||
            string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 4000 ||
            string.IsNullOrWhiteSpace(request.Category) || request.Category.Length > 100)
            return "Display name, title, description, and category are required and must fit their field limits.";
        if ((request.ServiceArea?.Length ?? 0) > 200 ||
            (request.ContactPhone?.Length ?? 0) > 40 ||
            (request.ContactLink?.Length ?? 0) > 500 ||
            (request.PhotoUrl?.Length ?? 0) > 1000)
            return "One or more optional fields exceed their maximum length.";
        if (!IsSafeWebUrl(request.ContactLink))
            return "Contact links must use HTTPS.";
        if (!IsUploadedPhotoUrl(request.PhotoUrl, httpRequest, configuration))
            return "Photos must be uploaded to this application before they can be published.";
        if (request.Offers is null || request.Offers.Count > 20)
            return "A showcase may contain at most 20 offers.";
        if (request.Offers.Any(o =>
                string.IsNullOrWhiteSpace(o.Name) || o.Name.Length > 160 ||
                string.IsNullOrWhiteSpace(o.Description) || o.Description.Length > 1000 ||
                (o.Details?.Length ?? 0) > 1000 ||
                (o.Price is < 0 or > 9999999999.99m) ||
                (!o.PriceOnRequest && o.Price is null)))
            return "Each offer needs a name, description, and either a price or 'price on request'.";
        return null;
    }

    private static bool IsSafeWebUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttps ||
                (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)) &&
            string.IsNullOrEmpty(uri.UserInfo);
    }

    private static bool IsUploadedPhotoUrl(
        string? value, HttpRequest request, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var photoUri) ||
            (photoUri.Scheme != Uri.UriSchemeHttps &&
                !(photoUri.Scheme == Uri.UriSchemeHttp && photoUri.IsLoopback)) ||
            !string.IsNullOrEmpty(photoUri.UserInfo))
            return false;

        var configuredPrefix = configuration["FileStorage:UrlPrefix"]?.TrimEnd('/');
        var uploadOrigin = string.IsNullOrWhiteSpace(configuredPrefix)
            ? $"{request.Scheme}://{request.Host}"
            : configuredPrefix;
        return Uri.TryCreate(uploadOrigin, UriKind.Absolute, out var uploadUri) &&
            string.Equals(photoUri.Authority, uploadUri.Authority, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> CreateSlugAsync(
        AppDbContext db, Guid tenantId, string title, CancellationToken ct)
    {
        var normalized = title.Normalize(NormalizationForm.FormD);
        var ascii = new string(normalized.Where(c =>
            CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        var baseSlug = NonAlphanumericRegex().Replace(ascii.ToLowerInvariant(), "-").Trim('-');
        if (baseSlug.Length > 100)
            baseSlug = baseSlug[..100].TrimEnd('-');
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "showcase";
        var slug = baseSlug;
        while (await db.CommunityShowcases.AnyAsync(s => s.TenantId == tenantId && s.Slug == slug, ct))
            slug = $"{baseSlug}-{Guid.NewGuid():N}"[..Math.Min(baseSlug.Length + 9, 109)];
        return slug;
    }

    private static string? CleanOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Guid? GetTenantId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst("tenant_id")?.Value, out var id) ? id : null;

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("sub") ??
            principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();
}
