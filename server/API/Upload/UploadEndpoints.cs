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

            var ext = Path.GetExtension(file.FileName);
            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var uploadsDir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
            Directory.CreateDirectory(uploadsDir);

            var filePath = Path.Combine(uploadsDir, uniqueName);
            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream);

            var baseUrl = $"{request.Scheme}://{request.Host}";
            var fileUrl = $"{baseUrl}/uploads/{uniqueName}";

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
