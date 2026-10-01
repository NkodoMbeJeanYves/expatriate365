using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Auth.DTOs;
using server.Application.Common;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Auth.Commands;

public record RegisterCommand(RegisterRequest Dto) : IRequest<ServiceResult<LoginResponse>>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Dto.AssociationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dto.Slug).NotEmpty().MaximumLength(100).Matches("^[a-z0-9-]+$");
        RuleFor(x => x.Dto.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Dto.ContactEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.Dto.ContactEmail));
        RuleFor(x => x.Dto.Password).NotEmpty().MinimumLength(8);
    }
}

public class RegisterCommandHandler(
    AppDbContext db,
    JwtService jwt,
    ILogger<RegisterCommandHandler> log,
    PermissionResolverService permissionResolver,
    IEmailService emailService,
    IConfiguration config)
    : IRequestHandler<RegisterCommand, ServiceResult<LoginResponse>>
{
    public async Task<ServiceResult<LoginResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        log.LogInformation("Register attempt: {Email}", dto.Email);

        if (await db.Tenants.AnyAsync(t => t.Slug == dto.Slug, ct))
            return ServiceResult<LoginResponse>.Failure("Ce slug est déjà utilisé.");

        if (await db.Users.AnyAsync(u => u.Email == dto.Email, ct))
            return ServiceResult<LoginResponse>.Failure("Cet email est déjà enregistré.");

        var tenant = new Tenant
        {
            Id           = Guid.NewGuid(),
            Name         = dto.AssociationName,
            Slug         = dto.Slug,
            CountryCode  = dto.CountryCode,
            BaseCurrency = dto.BaseCurrency,
        };

        var (plain, hash) = jwt.GenerateRefreshToken();
        var user = new User
        {
            Id              = Guid.NewGuid(),
            TenantId        = tenant.Id,
            Email           = dto.Email.ToLowerInvariant(),
            PasswordHash    = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName       = dto.FirstName,
            LastName        = dto.LastName,
            Phone           = dto.Phone,
            ContactEmail    = dto.ContactEmail?.ToLowerInvariant(),
            Role            = "org_admin",
            EmailVerifiedAt = DateTime.UtcNow,
            RefreshTokenHash      = hash,
            RefreshTokenExpiresAt = jwt.RefreshTokenExpiry(),
        };

        db.Tenants.Add(tenant);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        await permissionResolver.SeedForTenantAsync(tenant.Id, ct);

        // Envoyer confirmation de création de compte
        var loginUrl = config["FrontendBaseUrl"] ?? config["App:BaseUrl"] ?? "https://app.expatriate365.mu";
        if (!string.IsNullOrWhiteSpace(user.ContactEmail))
            _ = emailService.SendAsync(
                user.ContactEmail,
                user.FullName,
                $"Bienvenue sur Expatriate365 — {tenant.Name}",
                EmailTemplates.WelcomeOrgAdmin(user.FullName, tenant.Name, user.Email, dto.Password, loginUrl),
                ct);

        var permissions = await permissionResolver.ResolveAsync(user.Role, tenant.Id, ct);
        log.LogInformation("Registered tenant {TenantId} and user {UserId}", tenant.Id, user.Id);
        return ServiceResult<LoginResponse>.Success(new LoginResponse(
            jwt.GenerateAccessToken(user, permissions),
            plain,
            jwt.AccessExpirySeconds,
            jwt.ToUserInfo(user)
        ));
    }
}
