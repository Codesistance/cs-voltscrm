using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.Installments;

public sealed record InstallmentDto(
    Guid Id,
    MoneyDto Amount,
    DateTimeOffset DueDate,
    DateTimeOffset? PaidDate,
    string Status);

public sealed record InstallmentPlanListItemDto(
    Guid Id,
    Guid CustomerId,
    Guid CustomerSubscriptionId,
    string CustomerName,
    string CustomerAccountNumber,
    MoneyDto TotalAmount,
    MoneyDto DepositAmount,
    MoneyDto RemainingAmount,
    DateTimeOffset StartDate,
    int InstallmentCount);

public sealed record InstallmentPlanDto(
    Guid Id,
    Guid CustomerId,
    Guid CustomerSubscriptionId,
    string CustomerName,
    string CustomerAccountNumber,
    MoneyDto TotalAmount,
    MoneyDto DepositAmount,
    MoneyDto RemainingAmount,
    DateTimeOffset StartDate,
    IReadOnlyList<InstallmentDto> Installments,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
