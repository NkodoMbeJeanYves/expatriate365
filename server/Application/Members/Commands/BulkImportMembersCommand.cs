using MediatR;
using server.Application.Common;
using server.Application.Members.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Members.Commands;

public record BulkImportMemberRow(
    string FirstName, string LastName,
    string? Email, string? ContactEmail, string? Phone,
    string? JoinedDate, string? Address, string? Profession,
    string? DateOfBirth, string? Gender);

public record BulkImportMembersCommand(Guid TenantId, List<BulkImportMemberRow> Rows)
    : IRequest<ServiceResult<BulkImportMembersResult>>;

public record BulkImportMembersResult(int Created, int Skipped, List<BulkImportRowError> Errors);

public record BulkImportRowError(int Row, string FirstName, string LastName, string Error);

public class BulkImportMembersHandler(AppDbContext db, IMediator mediator, ILogger<BulkImportMembersHandler> log)
    : IRequestHandler<BulkImportMembersCommand, ServiceResult<BulkImportMembersResult>>
{
    public async Task<ServiceResult<BulkImportMembersResult>> Handle(BulkImportMembersCommand request, CancellationToken ct)
    {
        var created = 0;
        var skipped = 0;
        var errors  = new List<BulkImportRowError>();

        for (var i = 0; i < request.Rows.Count; i++)
        {
            var row = request.Rows[i];
            if (string.IsNullOrWhiteSpace(row.FirstName) || string.IsNullOrWhiteSpace(row.LastName))
            {
                errors.Add(new(i + 1, row.FirstName ?? "", row.LastName ?? "", "first_name and last_name are required."));
                skipped++;
                continue;
            }

            var dto = new CreateMemberRequest(
                row.FirstName.Trim(), row.LastName.Trim(),
                row.Email, row.ContactEmail, row.Phone,
                CategoryId: null,
                JoinedDate: row.JoinedDate ?? DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                ExpiryDate: null, PhotoUrl: null,
                row.Address, row.Profession, row.DateOfBirth, row.Gender,
                EmergencyContactName: null, EmergencyContactPhone: null);

            var result = await mediator.Send(new CreateMemberCommand(request.TenantId, dto), ct);
            if (result.IsSuccess)
            {
                created++;
                log.LogInformation("BulkImport: created member {FirstName} {LastName}", row.FirstName, row.LastName);
            }
            else
            {
                errors.Add(new(i + 1, row.FirstName, row.LastName, result.ErrorMessage ?? "Unknown error"));
                skipped++;
            }
        }

        return ServiceResult<BulkImportMembersResult>.Success(new(created, skipped, errors));
    }
}
