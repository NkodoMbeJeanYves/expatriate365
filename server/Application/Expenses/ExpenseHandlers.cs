using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Expenses;

// --- DTOs ---

public record ExpenseDto(
    string Id,
    string Label,
    string? Description,
    string Category,
    decimal Amount,
    string Currency,
    string Date,
    string Status,
    string? ValidatedBy,
    string? ValidatedAt,
    string CreatedAt
);

public record CreateExpenseRequest(
    string Label,
    string? Description,
    string Category,
    decimal Amount,
    string Currency,
    string Date
);

public record UpdateExpenseRequest(
    string Label,
    string? Description,
    string Category,
    decimal Amount,
    string Currency,
    string Date
);

public record ExpenseStatsDto(
    decimal TotalAmount,
    int TotalCount,
    int PendingCount,
    int ValidatedCount,
    int RejectedCount
);

// --- Queries ---

public record ListExpensesQuery(Guid TenantId, int Page = 1, int Limit = 20,
    string? Status = null, string? Category = null,
    string? From = null, string? To = null)
    : IRequest<PagedResult<ExpenseDto>>;

public class ListExpensesQueryHandler(AppDbContext db)
    : IRequestHandler<ListExpensesQuery, PagedResult<ExpenseDto>>
{
    public async Task<PagedResult<ExpenseDto>> Handle(ListExpensesQuery request, CancellationToken ct)
    {
        var q = db.Expenses.Where(e => e.TenantId == request.TenantId && e.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Status))
            q = q.Where(e => e.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.Category))
            q = q.Where(e => e.Category == request.Category);
        if (DateTime.TryParse(request.From, out var from))
            q = q.Where(e => e.Date >= from);
        if (DateTime.TryParse(request.To, out var to))
            q = q.Where(e => e.Date <= to);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(e => e.Date)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .Include(e => e.Validator)
            .Select(e => ToDto(e))
            .ToListAsync(ct);

        return PagedResult<ExpenseDto>.Create(items, request.Page, request.Limit, total);
    }

    public static ExpenseDto ToDto(Expense e) => new(
        e.Id.ToString(), e.Label, e.Description, e.Category,
        e.Amount, e.Currency,
        e.Date.ToString("yyyy-MM-dd"),
        e.Status,
        e.Validator != null ? $"{e.Validator.FirstName} {e.Validator.LastName}" : null,
        e.ValidatedAt?.ToString("O"),
        e.CreatedAt.ToString("O"));
}

public record GetExpenseStatsQuery(Guid TenantId) : IRequest<ExpenseStatsDto>;

public class GetExpenseStatsQueryHandler(AppDbContext db)
    : IRequestHandler<GetExpenseStatsQuery, ExpenseStatsDto>
{
    public async Task<ExpenseStatsDto> Handle(GetExpenseStatsQuery request, CancellationToken ct)
    {
        var expenses = await db.Expenses
            .Where(e => e.TenantId == request.TenantId && e.IsActive)
            .ToListAsync(ct);

        return new ExpenseStatsDto(
            expenses.Where(e => e.Status == "validated").Sum(e => e.Amount),
            expenses.Count,
            expenses.Count(e => e.Status == "pending"),
            expenses.Count(e => e.Status == "validated"),
            expenses.Count(e => e.Status == "rejected")
        );
    }
}

// --- Commands ---

public record CreateExpenseCommand(Guid TenantId, CreateExpenseRequest Dto) : IRequest<ServiceResult<ExpenseDto>>;

public class CreateExpenseCommandHandler(AppDbContext db, ILogger<CreateExpenseCommandHandler> log)
    : IRequestHandler<CreateExpenseCommand, ServiceResult<ExpenseDto>>
{
    public async Task<ServiceResult<ExpenseDto>> Handle(CreateExpenseCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        if (!DateTime.TryParse(dto.Date, out var date))
            return ServiceResult<ExpenseDto>.Failure("Date invalide.", "errors.common.invalid_date");

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Label = dto.Label,
            Description = dto.Description,
            Category = dto.Category,
            Amount = dto.Amount,
            Currency = dto.Currency,
            Date = date,
            Status = "pending",
        };

        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Expense {Id} created: {Label}", expense.Id, expense.Label);
        return ServiceResult<ExpenseDto>.Success(ListExpensesQueryHandler.ToDto(expense));
    }
}

public record UpdateExpenseCommand(Guid TenantId, Guid Id, UpdateExpenseRequest Dto) : IRequest<ServiceResult<ExpenseDto>>;

public class UpdateExpenseCommandHandler(AppDbContext db, ILogger<UpdateExpenseCommandHandler> log)
    : IRequestHandler<UpdateExpenseCommand, ServiceResult<ExpenseDto>>
{
    public async Task<ServiceResult<ExpenseDto>> Handle(UpdateExpenseCommand request, CancellationToken ct)
    {
        var expense = await db.Expenses.Include(e => e.Validator)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == request.TenantId, ct);
        if (expense is null) return ServiceResult<ExpenseDto>.Failure("Dépense introuvable.", "errors.expense.not_found");
        if (expense.Status == "validated")
            return ServiceResult<ExpenseDto>.Failure("Impossible de modifier une dépense validée.", "errors.expense.cannot_edit");

        var dto = request.Dto;
        if (!DateTime.TryParse(dto.Date, out var date))
            return ServiceResult<ExpenseDto>.Failure("Date invalide.", "errors.common.invalid_date");

        expense.Label = dto.Label;
        expense.Description = dto.Description;
        expense.Category = dto.Category;
        expense.Amount = dto.Amount;
        expense.Currency = dto.Currency;
        expense.Date = date;

        await db.SaveChangesAsync(ct);
        log.LogInformation("Expense {Id} updated", expense.Id);
        return ServiceResult<ExpenseDto>.Success(ListExpensesQueryHandler.ToDto(expense));
    }
}

public record ValidateExpenseCommand(Guid TenantId, Guid Id, Guid ValidatorId, bool Approve) : IRequest<ServiceResult<ExpenseDto>>;

public class ValidateExpenseCommandHandler(AppDbContext db, ILogger<ValidateExpenseCommandHandler> log)
    : IRequestHandler<ValidateExpenseCommand, ServiceResult<ExpenseDto>>
{
    public async Task<ServiceResult<ExpenseDto>> Handle(ValidateExpenseCommand request, CancellationToken ct)
    {
        var expense = await db.Expenses.Include(e => e.Validator)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == request.TenantId, ct);
        if (expense is null) return ServiceResult<ExpenseDto>.Failure("Dépense introuvable.", "errors.expense.not_found");
        if (expense.Status != "pending")
            return ServiceResult<ExpenseDto>.Failure("Seules les dépenses en attente peuvent être validées.", "errors.expense.invalid_status");

        expense.Status = request.Approve ? "validated" : "rejected";
        expense.ValidatedBy = request.ValidatorId;
        expense.ValidatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("Expense {Id} {Action}", expense.Id, request.Approve ? "validated" : "rejected");
        return ServiceResult<ExpenseDto>.Success(ListExpensesQueryHandler.ToDto(expense));
    }
}

public record DeleteExpenseCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<bool>>;

public class DeleteExpenseCommandHandler(AppDbContext db, ILogger<DeleteExpenseCommandHandler> log)
    : IRequestHandler<DeleteExpenseCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeleteExpenseCommand request, CancellationToken ct)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(
            e => e.Id == request.Id && e.TenantId == request.TenantId, ct);
        if (expense is null) return ServiceResult<bool>.Failure("Dépense introuvable.", "errors.expense.not_found");
        if (expense.Status == "validated")
            return ServiceResult<bool>.Failure("Impossible de supprimer une dépense validée.", "errors.expense.cannot_edit");

        expense.IsActive = false;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Expense {Id} deleted", expense.Id);
        return ServiceResult<bool>.Success(true);
    }
}
