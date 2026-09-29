using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Auth.Commands;

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? Phone,
    string? ContactEmail
);

public record UpdateProfileCommand(Guid UserId, UpdateProfileRequest Dto) : IRequest<ServiceResult<bool>>;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Dto.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.ContactEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.Dto.ContactEmail));
    }
}

public class UpdateProfileCommandHandler(AppDbContext db, ILogger<UpdateProfileCommandHandler> log)
    : IRequestHandler<UpdateProfileCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([request.UserId], ct);
        if (user is null)
            return ServiceResult<bool>.Failure("Utilisateur introuvable.");

        user.FirstName    = request.Dto.FirstName;
        user.LastName     = request.Dto.LastName;
        user.Phone        = request.Dto.Phone;
        user.ContactEmail = string.IsNullOrWhiteSpace(request.Dto.ContactEmail)
            ? null
            : request.Dto.ContactEmail.ToLowerInvariant();
        user.UpdatedAt    = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("User {UserId} updated profile", user.Id);
        return ServiceResult<bool>.Success(true);
    }
}
