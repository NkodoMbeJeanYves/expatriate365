using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using server.Application.SuperAdmin;
using server.Application.TenantSettings;
using server.Domain.Entities;
using server.Infrastructure.Services;

namespace server.Tests;

public sealed class TenantCurrencySettingsTests
{
    [Fact]
    public async Task SettingsUpdate_RejectsMismatchBeforeAnyMutation()
    {
        await using var db = TestDbFactory.Create();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Original", Slug = "original", CountryCode = "MU", BaseCurrency = "MUR" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        var handler = new UpdateTenantSettingsHandler(db, new TestEnvironment(), Config(), Service("covered"), new AuditService(db));
        var result = await handler.Handle(new(tenant.Id, new("Changed", "EUR", null, "MU", null)), default);
        Assert.False(result.IsSuccess);
        Assert.Equal("errors.tenant.currency_mismatch", result.ErrorCode);
        Assert.Equal("Original", tenant.Name);
        Assert.Equal("MUR", tenant.BaseCurrency);
    }

    [Fact]
    public async Task CountryOnlyUpdate_DoesNotSilentlyConvertCurrency()
    {
        await using var db = TestDbFactory.Create();
        var tenant = new Tenant { Id = Guid.NewGuid(), CountryCode = "MU", BaseCurrency = "MUR" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        var handler = new UpdateTenantSettingsHandler(db, new TestEnvironment(), Config(), Service("covered"), new AuditService(db));
        var result = await handler.Handle(new(tenant.Id, new(null, null, null, "RS", null)), default);
        Assert.False(result.IsSuccess);
        Assert.Equal("MU", tenant.CountryCode);
        Assert.Equal("MUR", tenant.BaseCurrency);
    }

    [Theory]
    [InlineData("covered")]
    [InlineData("not_covered")]
    [InlineData("unavailable")]
    public async Task SettingsUpdate_NormalizesAndSavesValidCurrencyRegardlessOfCoverage(string status)
    {
        await using var db = TestDbFactory.Create();
        var tenant = new Tenant { Id = Guid.NewGuid(), CountryCode = "MU", BaseCurrency = "MUR" };
        db.Tenants.Add(tenant);
        var request = new TransferAssistanceRequest { Id = Guid.NewGuid(), TenantId = tenant.Id, AmountMur = 1000 };
        db.TransferAssistanceRequests.Add(request);
        await db.SaveChangesAsync();
        var handler = new UpdateTenantSettingsHandler(db, new TestEnvironment(), Config(), Service(status), new AuditService(db));
        var actor = Guid.NewGuid();
        var result = await handler.Handle(new(tenant.Id, new(null, " rsd ", "RSD", " rs ", null), actor), default);
        Assert.True(result.IsSuccess);
        Assert.Equal("RSD", result.Data!.BaseCurrency);
        Assert.Equal("RS", result.Data.CountryCode);
        Assert.Equal(status, result.Data.CurrencyValidation!.CoverageStatus);
        Assert.Equal(1000, (await db.TransferAssistanceRequests.SingleAsync()).AmountMur);
        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal(actor, audit.UserId);
        Assert.Equal(tenant.Id, audit.TenantId);
        Assert.Equal("tenant_currency_configuration_updated", audit.Action);
    }

    [Fact]
    public async Task NonMonetaryUpdate_PreservesExistingConfiguration()
    {
        await using var db = TestDbFactory.Create();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Original", CountryCode = "XX", BaseCurrency = "EUR" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        var handler = new UpdateTenantSettingsHandler(db, new TestEnvironment(), Config(), Service("unavailable"), new AuditService(db));
        var result = await handler.Handle(new(tenant.Id, new("New name", null, null, null, null)), default);
        Assert.True(result.IsSuccess);
        Assert.Equal("New name", tenant.Name);
        Assert.Equal("XX", tenant.CountryCode);
    }

    [Fact]
    public async Task AssociationCreation_RejectsMismatchEvenWithoutUiValidation()
    {
        await using var db = TestDbFactory.Create();
        var handler = new CreateTenantCommandHandler(db, NullLogger<CreateTenantCommandHandler>.Instance,
            new PermissionResolverService(db), new TestEmailService(), Config(), Service("covered"));
        var result = await handler.Handle(new(new("Test", "test", "Test", "Admin",
            "admin@example.test", "ValidPass123!", CountryCode: "MU", BaseCurrency: "EUR")), default);
        Assert.False(result.IsSuccess);
        Assert.Equal("errors.tenant.currency_mismatch", result.ErrorCode);
        Assert.Empty(db.Tenants);
        Assert.Empty(db.Users);
    }

    [Theory]
    [InlineData("covered")]
    [InlineData("unavailable")]
    public async Task AssociationCreation_SavesSerbianCurrencyAndReturnsCoverage(string status)
    {
        await using var db = TestDbFactory.Create();
        var handler = new CreateTenantCommandHandler(db, NullLogger<CreateTenantCommandHandler>.Instance,
            new PermissionResolverService(db), new TestEmailService(), Config(), Service(status));
        var result = await handler.Handle(new(new("Test", "test", "Test", "Admin",
            "admin@example.test", "ValidPass123!", CountryCode: " rs ", BaseCurrency: " rsd ")), default);
        Assert.True(result.IsSuccess);
        Assert.Equal("RS", result.Data!.CountryCode);
        Assert.Equal("RSD", result.Data.BaseCurrency);
        Assert.Equal(status, result.Data.CurrencyValidation!.CoverageStatus);
    }

    private static IConfiguration Config() => new ConfigurationBuilder().Build();
    private static CurrencyValidationService Service(string status)
        => new(new TestCatalogue(status), NullLogger<CurrencyValidationService>.Instance);

    private sealed class TestCatalogue(string status) : ICurrencyCatalogue
    {
        public Task<CurrencyCoverage> CheckAsync(string currency, CancellationToken ct)
            => Task.FromResult(new CurrencyCoverage(status, null));
    }

    private sealed class TestEmailService : IEmailService
    {
        public Task SendAsync(string to, string name, string subject, string html, CancellationToken ct = default)
            => throw new NotSupportedException("No contact email supplied in this test.");
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
