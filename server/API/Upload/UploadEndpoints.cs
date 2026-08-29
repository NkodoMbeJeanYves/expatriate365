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

    public static void MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
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

            // Lecture explicite depuis la query string (multipart/form-data ne bind pas les params automatiquement)
            var folder = request.Query["folder"].FirstOrDefault();
            var allowedFolders = new HashSet<string> { "uploads", "logos", "photos", "documents" };
            var targetFolder = allowedFolders.Contains(folder ?? "") ? folder! : "uploads";
Console.WriteLine($"[Upload] User '{principal.Identity?.Name}' uploading to folder '{targetFolder}'");

Console.WriteLine($"[Upload] User '{principal.Identity?.Name}' uploading to folder '{targetFolder}'");

Console.WriteLine($"[Upload] User '{principal.Identity?.Name}' uploading to folder '{targetFolder}'");

            var ext = Path.GetExtension(file.FileName);
            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var wwwroot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var targetDir = Path.Combine(wwwroot, targetFolder);
            Directory.CreateDirectory(targetDir);

            var filePath = Path.Combine(targetDir, uniqueName);
            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream);

            // Use configured public URL prefix (FileStorage__UrlPrefix) so the stored
            // URL is always the public domain, not the internal Kestrel host.
            var urlPrefix = config["FileStorage:UrlPrefix"]?.TrimEnd('/')
                ?? $"{request.Scheme}://{request.Host}";
                Console.WriteLine($"[Upload] Using URL prefix '{urlPrefix}' for file access: '{request.Scheme}://{request.Host}'");
            var fileUrl = $"{urlPrefix}/{targetFolder}/{uniqueName}";
Console.WriteLine($"[Upload] File saved to '{filePath}', accessible at '{fileUrl}'");
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
