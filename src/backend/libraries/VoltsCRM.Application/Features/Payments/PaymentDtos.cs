using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.Payments;

public sealed record PaymentAllocationDto(Guid Id, Guid? InvoiceId, Guid? InstallmentId, decimal Amount);

public sealed record PaymentListItemDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    MoneyDto Amount,
    MoneyDto NetAmount,
    string Method,
    string Channel,
    string Status,
    DateTimeOffset PaymentDate,
    string? PlatformReference);

public sealed record PaymentDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerAccountNumber,
    MoneyDto Amount,
    decimal DiscountApplied,
    MoneyDto NetAmount,
    string Method,
    string Channel,
    string Status,
    string? PlatformProvider,
    string? PlatformReference,
    string? CollectedByAgentId,
    DateTimeOffset PaymentDate,
    IReadOnlyList<PaymentAllocationDto> Allocations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
