using System.Diagnostics;
using System.IO.Compression;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using server.Application.SuperAdmin;

namespace server.API.SuperAdmin;

public static class SuperAdminEndpoints
{
    public static void MapSuperAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/superadmin")
            .WithTags("SuperAdmin")
            .RequireAuthorization();

        group.MapGet("/tenants", async (HttpContext ctx, IMediator mediator) =>
        {
            if (!IsSuperAdmin(ctx)) return Results.Forbid();
            var result = await mediator.Send(new ListTenantsQuery());
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("ListTenants")
        .WithSummary("List all associations (super_admin only)");

        group.MapPost("/tenants", async (HttpContext ctx, [FromBody] CreateTenantRequest dto, IMediator mediator) =>
        {
            if (!IsSuperAdmin(ctx)) return Results.Forbid();
            var result = await mediator.Send(new CreateTenantCommand(dto));
            return result.IsSuccess ? Results.Created($"/api/v1/superadmin/tenants", result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("CreateTenant")
        .WithSummary("Create a new association with its org_admin (super_admin only)");

        group.MapPatch("/tenants/{id}/toggle-active", async (HttpContext ctx, Guid id, IMediator mediator) =>
        {
            if (!IsSuperAdmin(ctx)) return Results.Forbid();
            var result = await mediator.Send(new ToggleTenantActiveCommand(id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("ToggleTenantActive")
        .WithSummary("Activate or deactivate an association (super_admin only)");

        group.MapGet("/backup", async (HttpContext ctx, IConfiguration config, ILoggerFactory logFactory) =>
        {
            if (!IsSuperAdmin(ctx)) return Results.Forbid();

            var log = logFactory.CreateLogger("SuperAdmin.Backup");
            var connStr = config.GetConnectionString("MySql");
            if (string.IsNullOrWhiteSpace(connStr))
                return Results.Problem("MySQL connection string not configured.");

            MySqlConnectionStringBuilder csb;
            try { csb = new MySqlConnectionStringBuilder(connStr); }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to parse MySQL connection string for backup");
                return Results.Problem("Invalid MySQL connection string.");
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var filename  = $"expatriate365_backup_{timestamp}.sql.gz";

            var psi = new ProcessStartInfo
            {
                FileName  = "mysqldump",
                Arguments = $"--host={csb.Server} --port={csb.Port} --user={csb.UserID} " +
                            $"--single-transaction --routines --triggers --add-drop-table {csb.Database}",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };
            psi.Environment["MYSQL_PWD"] = csb.Password;

            Process process;
            try { process = Process.Start(psi)!; }
            catch (Exception ex)
            {
                log.LogError(ex, "mysqldump not found or failed to start");
                return Results.Problem("mysqldump executable not found on server.");
            }

            ctx.Response.ContentType = "application/gzip";
            ctx.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{filename}\"");

            try
            {
                await using var gzip = new GZipStream(ctx.Response.Body, CompressionLevel.Optimal, leaveOpen: true);
                await process.StandardOutput.BaseStream.CopyToAsync(gzip, ctx.RequestAborted);
            }
            finally
            {
                await process.WaitForExitAsync(ctx.RequestAborted);
                if (process.ExitCode != 0)
                {
                    var err = await process.StandardError.ReadToEndAsync();
                    log.LogError("mysqldump exited with code {Code}: {Error}", process.ExitCode, err);
                }
                else
                {
                    log.LogInformation("Database backup {Filename} downloaded by super_admin", filename);
                }
                process.Dispose();
            }

            return Results.Empty;
        })
        .WithName("DownloadBackup")
        .WithSummary("Download a full MySQL dump as .sql.gz (super_admin only)");
    }

    private static bool IsSuperAdmin(HttpContext ctx)
    {
        var role = ctx.User.FindFirst("role")?.Value
                ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        return role == "super_admin";
    }
}
