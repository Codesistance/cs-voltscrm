using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.Invoices;

public sealed record InvoiceLineItemDto(
    Guid Id,
    string Description,
    decimal Amount,
    bool IsDiscount);

public sealed record InvoiceListItemDto(
    Guid Id,
    Guid CustomerId,
    Guid CustomerSubscriptionId,
    string CustomerName,
    string CustomerAccountNumber,
    int PeriodYear,
    int PeriodMonth,
    MoneyDto GrossAmount,
    MoneyDto DiscountAmount,
    MoneyDto AmountDue,
    MoneyDto AmountPaid,
    MoneyDto Balance,
    DateTimeOffset DueDate,
    string Status);

public sealed record InvoiceDto(
    Guid Id,
    Guid CustomerId,
    Guid CustomerSubscriptionId,
    string CustomerName,
    string CustomerAccountNumber,
    int PeriodYear,
    int PeriodMonth,
    MoneyDto GrossAmount,
    MoneyDto DiscountAmount,
    MoneyDto AmountDue,
    MoneyDto AmountPaid,
    MoneyDto Balance,
    DateTimeOffset DueDate,
    string Status,
    IReadOnlyList<InvoiceLineItemDto> LineItems,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PaymentAccountDto(
    Guid Id,
    Guid CustomerId,
    MoneyDto Balance,
    DateTimeOffset? LastPaymentDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
