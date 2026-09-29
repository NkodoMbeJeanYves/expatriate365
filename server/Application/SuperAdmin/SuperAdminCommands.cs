using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.SuperAdmin;

// ── Create tenant ─────────────────────────────────────────────────────────────

public record CreateTenantCommand(CreateTenantRequest Dto) : IRequest<ServiceResult<TenantSummaryDto>>;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Dto.AssociationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dto.Slug).NotEmpty().MaximumLength(100).Matches("^[a-z0-9-]+$");
        RuleFor(x => x.Dto.AdminFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.AdminLastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Dto.AdminContactEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.Dto.AdminContactEmail));
        RuleFor(x => x.Dto.AdminPassword).NotEmpty().MinimumLength(8);
    }
}

public class CreateTenantCommandHandler(
    AppDbContext db,
    ILogger<CreateTenantCommandHandler> log,
    PermissionResolverService permissionResolver,
    IEmailService emailService,
    IConfiguration config)
    : IRequestHandler<CreateTenantCommand, ServiceResult<TenantSummaryDto>>
{
    public async Task<ServiceResult<TenantSummaryDto>> Handle(CreateTenantCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        log.LogInformation("SuperAdmin creating tenant: {Slug}", dto.Slug);

        if (await db.Tenants.AnyAsync(t => t.Slug == dto.Slug, ct))
            return ServiceResult<TenantSummaryDto>.Failure("Ce slug est déjà utilisé.");

        if (await db.Users.AnyAsync(u => u.Email == dto.AdminEmail, ct))
            return ServiceResult<TenantSummaryDto>.Failure("Cet email est déjà enregistré.");

        var tenant = new Tenant
        {
            Id           = Guid.NewGuid(),
            Name         = dto.AssociationName,
            Slug         = dto.Slug,
            CountryCode  = dto.CountryCode,
            BaseCurrency = dto.BaseCurrency,
        };

        var admin = new User
        {
            Id              = Guid.NewGuid(),
            TenantId        = tenant.Id,
            Email           = dto.AdminEmail.ToLowerInvariant(),
            PasswordHash    = BCrypt.Net.BCrypt.HashPassword(dto.AdminPassword),
            FirstName       = dto.AdminFirstName,
            LastName        = dto.AdminLastName,
            Phone           = dto.Phone,
            ContactEmail    = dto.AdminContactEmail?.ToLowerInvariant(),
            Role            = "org_admin",
            EmailVerifiedAt = DateTime.UtcNow,
            Status          = "active",
            IsActive        = true,
        };

        db.Tenants.Add(tenant);
        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);

        await permissionResolver.SeedForTenantAsync(tenant.Id, ct);

        log.LogInformation("Tenant {TenantId} created by super_admin with admin {UserId}", tenant.Id, admin.Id);

        // Envoyer les identifiants à l'org_admin
        var loginUrl = config["App:BaseUrl"] ?? "https://app.expatriate365.mu";
        var notifEmail = admin.ContactEmail ?? admin.Email;
        _ = emailService.SendAsync(
            notifEmail,
            admin.FullName,
            $"Vos identifiants Expatriate365 — {tenant.Name}",
            EmailTemplates.WelcomeOrgAdmin(admin.FullName, tenant.Name, admin.Email, dto.AdminPassword, loginUrl),
            ct);

        return ServiceResult<TenantSummaryDto>.Success(new TenantSummaryDto(
            tenant.Id.ToString(),
            tenant.Name,
            tenant.Slug,
            tenant.CountryCode,
            tenant.BaseCurrency,
            tenant.IsActive,
            tenant.CreatedAt.ToString("O"),
            null,
            admin.Email,
            $"{admin.FirstName} {admin.LastName}",
            1
        ));
    }
}
