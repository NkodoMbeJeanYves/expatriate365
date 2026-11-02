using System.Security.Claims;

namespace server.API.Upload;

public static class UploadEndpoints
{
    private static readonly HashSet<string> AllowedMimeTypes =
    [
        "application/pdf",
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    ];
    private static readonly Dictionary<string, string> ShowcaseImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp",
    };
    private static readonly Dictionary<string, string> ShowcaseImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    public static void MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/downloads/{tenantId:guid}/showcases/{fileName}", (
            Guid tenantId,
            string fileName,
            IWebHostEnvironment env,
            HttpResponse response) =>
        {
            var extension = Path.GetExtension(fileName);
            if (!Guid.TryParse(Path.GetFileNameWithoutExtension(fileName), out _) ||
                !ShowcaseImageExtensions.TryGetValue(extension, out var contentType))
                return Results.NotFound();

            var directory = Path.GetFullPath(Path.Combine(
                env.ContentRootPath, "downloads", tenantId.ToString("D"), "showcases"));
            var filePath = Path.GetFullPath(Path.Combine(directory, fileName));
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!filePath.StartsWith(directory + Path.DirectorySeparatorChar, comparison) ||
                !File.Exists(filePath))
                return Results.NotFound();

            response.Headers["X-Content-Type-Options"] = "nosniff";
            return Results.File(filePath, contentType);
        })
        .WithTags("Upload")
        .WithSummary("Get a public showcase photo.")
        .AllowAnonymous();

        app.MapPost("/api/v1/upload", async (
            IFormFile file,
            IWebHostEnvironment env,
            IConfiguration config,
            ClaimsPrincipal principal,
            HttpRequest request) =>
        {
            if (!principal.Identity?.IsAuthenticated ?? true)
                return Results.Unauthorized();

            if (file.Length == 0)
                return Results.BadRequest(new { error = "No file provided." });

            if (file.Length > 20 * 1024 * 1024)
                return Results.BadRequest(new { error = "File exceeds 20 MB limit." });

            if (!AllowedMimeTypes.Contains(file.ContentType))
                return Results.BadRequest(new { error = $"File type '{file.ContentType}' not allowed." });

            var tenantIdClaim = principal.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrWhiteSpace(tenantIdClaim))
                return Results.Unauthorized();

            // Lecture explicite depuis la query string (multipart/form-data ne bind pas les params automatiquement)
            var folder = request.Query["folder"].FirstOrDefault();
            var allowedFolders = new HashSet<string> { "attachments", "branding", "avatars", "docs", "showcases" };
            var targetFolder = allowedFolders.Contains(folder ?? "") ? folder! : "attachments";
            if (targetFolder == "showcases" && !ShowcaseImageTypes.ContainsKey(file.ContentType))
                return Results.BadRequest(new { error = "Showcase photos must be JPEG, PNG, GIF, or WebP images." });

            var ext = targetFolder == "showcases"
                ? ShowcaseImageTypes[file.ContentType]
                : Path.GetExtension(file.FileName);
            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var downloadRootPath = "downloads";
            var targetDir = Path.Combine(env.ContentRootPath, downloadRootPath, tenantIdClaim, targetFolder);
            Directory.CreateDirectory(targetDir);

            var filePath = Path.Combine(targetDir, uniqueName);
            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream);

            var urlPrefix = config["FileStorage:UrlPrefix"]?.TrimEnd('/')
                ?? $"{request.Scheme}://{request.Host}";
            var fileUrl = $"{urlPrefix}/{downloadRootPath}/{tenantIdClaim}/{targetFolder}/{uniqueName}";
            return Results.Ok(new
            {
                file_url = fileUrl,
                file_name = file.FileName,
                file_size_bytes = file.Length,
                mime_type = file.ContentType,
            });
        })
        .WithTags("Upload")
        .RequireAuthorization()
        .DisableAntiforgery();
    }
}
