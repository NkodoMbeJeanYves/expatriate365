namespace server.Application.Finances.DTOs;

public record FinanceSummaryDto(
    decimal TotalCollected,
    decimal TotalExpected,
    decimal Balance,
    decimal CollectionRate,
    int TotalTransactions);

public record FinanceTransactionDto(
    string Id, string Type, string MemberName, string MembershipNumber,
    decimal Amount, string Currency, string Status, string Date, string? Description);

public record FinanceTransactionFilters(
    int Page, int Limit, string? Type, string? Status, string? From, string? To);
