using System.Reflection;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using server.Api.Auth;
using server.Api.Members;
using server.API.Contributions;
using server.API.Payments;
using server.API.Welfare;
using server.API.Events;
using server.API.Meetings;
using server.API.Elections;
using server.API.Communications;
using server.API.Documents;
using server.API.Governance;
using server.API.Admin;
using server.API.Analytics;
using server.API.Upload;
using server.API.Finances;
using server.API.Tenant;
using server.API.Roles;
using Microsoft.AspNetCore.Authorization;
using server.Infrastructure.Auth;
using server.Infrastructure.BackgroundServices;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;
using Microsoft.Extensions.FileProviders;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.UseUrls("http://*:5000", "https://*:5001");

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console());

    builder.Services.AddOpenApi();

    // Global JSON: snake_case for all Minimal API responses and requests
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
        options.SerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });

    builder.Services.ConfigureHttpJsonOptions(o =>
        o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower);

    builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    var cs = builder.Configuration.GetConnectionString("MySql")
        ?? throw new InvalidOperationException("Connection string 'Default' not found.");
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseMySql(cs, ServerVersion.AutoDetect(cs)));

    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

    builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

    builder.Services.AddHostedService<ChargeGenerationBackgroundService>();
    builder.Services.AddSingleton<JwtService>();

    var jwtSecret = builder.Configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("Jwt:SecretKey not configured");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };
        });

    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
    builder.Services.AddAuthorization();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseSerilogRequestLogging();
    app.UseCors();
    // app.UseStaticFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
        RequestPath = ""
    });

    app.UseAuthentication();
    app.UseAuthorization();

    {
        using var scope = app.Services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await ApplyMigrationsAsync(db);

        if (args.Contains("--reset"))
        {
            Console.WriteLine("[Reset] Dropping and recreating schema...");
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            await DbSeeder.BootstrapAsync(db, config);
            Console.WriteLine("[Reset] Done — schema recreated, roles and super_admin seeded. Exiting.");
            return;
        }

        if (args.Contains("--seed"))
        {
            await DbSeeder.ResetAndSeedAsync(db, config);
            Console.WriteLine("[Seeder] --seed complete. Exiting.");
            return;
        }

        await DbSeeder.BootstrapAsync(db, config);
    }

    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
    app.MapUploadEndpoints();

    app.MapAuthEndpoints();
    app.MapMemberEndpoints();
    app.MapContributionEndpoints();
    app.MapPaymentEndpoints();
    app.MapWelfareEndpoints();
    app.MapEventEndpoints();
    app.MapMeetingEndpoints();
    app.MapElectionEndpoints();
    app.MapCommunicationEndpoints();
    app.MapDocumentEndpoints();
    app.MapGovernanceEndpoints();
    app.MapAdminEndpoints();
    app.MapAnalyticsEndpoints();
    app.MapFinanceEndpoints();
    app.MapTenantEndpoints();
    app.MapRoleEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}

// ── Helpers ───────────────────────────────────────────────────────────────────

static async Task ApplyMigrationsAsync(AppDbContext db)
{
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
    if (pending.Count == 0)
    {
        Console.WriteLine("[Migration] Already up to date.");
        return;
    }

    // Check whether tables already exist (kept database scenario)
    var tablesExist = await DatabaseHasTablesAsync(db);
    if (tablesExist)
    {
        // Tables exist but migration history doesn't know about them.
        // Mark all pending migrations as applied without running their SQL.
        Console.WriteLine("[Migration] Tables already exist — marking pending migrations as applied.");
        foreach (var migration in pending)
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (`MigrationId` varchar(150) NOT NULL, `ProductVersion` varchar(32) NOT NULL, CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`));");
            await db.Database.ExecuteSqlRawAsync(
                $"INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('{migration}', '10.0.0');");
        }
        Console.WriteLine($"[Migration] {pending.Count} migration(s) marked as applied.");
    }
    else
    {
        Console.WriteLine($"[Migration] Applying {pending.Count} pending migration(s)...");
        await db.Database.MigrateAsync();
        Console.WriteLine("[Migration] Done.");
    }
}

static async Task<bool> DatabaseHasTablesAsync(AppDbContext db)
{
    // Compare tables present in the DB against tables declared in the EF model.
    // If all model tables are found, the schema is considered complete.
    var modelTables = db.Model.GetEntityTypes()
        .Select(e => e.GetTableName())
        .Where(t => t is not null)
        .Select(t => t!)
        .Distinct()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync();
    try
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = DATABASE();";
        using var reader = await cmd.ExecuteReaderAsync();
        var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync())
            existingTables.Add(reader.GetString(0));

        var missing = modelTables.Except(existingTables).ToList();
        if (missing.Count > 0)
            Console.WriteLine($"[Migration] Tables manquantes : {string.Join(", ", missing)}");

        return missing.Count == 0;
    }
    finally
    {
        await conn.CloseAsync();
    }
}
