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
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        await DbSeeder.SeedSuperAdminAsync(db);
        await RoleSeeder.SeedRolesAsync(db);

        if (args.Contains("--reseed"))
            await DbSeeder.ReseedAsync(db);
        else if (args.Contains("--seed"))
            await DbSeeder.SeedAsync(db);

        await DbSeeder.SeedSuperAdminAsync(db);
        await RoleSeeder.SeedRolesAsync(db);
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
