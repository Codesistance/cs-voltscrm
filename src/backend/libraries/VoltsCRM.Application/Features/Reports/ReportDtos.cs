using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.Reports;

public sealed record DashboardSummaryDto(
    int ActiveCustomers,
    MoneyDto OutstandingBalance,
    MoneyDto CollectionsMtd,
    int OverdueInvoices);

public sealed record CollectionSummaryItemDto(
    DateOnly Date,
    int PaymentCount,
    MoneyDto TotalAmount);

public sealed record CollectionSummaryDto(
    DateOnly From,
    DateOnly To,
    MoneyDto TotalCollected,
    IReadOnlyList<CollectionSummaryItemDto> Daily);

public sealed record AgingBucketDto(
    string Bucket,
    int InvoiceCount,
    MoneyDto TotalBalance);

public sealed record AgingReportDto(IReadOnlyList<AgingBucketDto> Buckets);

public sealed record StatementInvoiceDto(
    Guid Id,
    DateTimeOffset DueDate,
    MoneyDto AmountDue,
    MoneyDto AmountPaid,
    MoneyDto Balance,
    string Status);

public sealed record StatementPaymentDto(
    Guid Id,
    DateTimeOffset PaymentDate,
    MoneyDto Amount,
    MoneyDto NetAmount,
    string Method,
    string Status,
    string? Reference);

public sealed record CustomerStatementDto(
    Guid CustomerId,
    string CustomerName,
    string CustomerAccountNumber,
    DateOnly? From,
    DateOnly? To,
    MoneyDto TotalInvoiced,
    MoneyDto TotalPaid,
    MoneyDto OutstandingBalance,
    IReadOnlyList<StatementInvoiceDto> Invoices,
    IReadOnlyList<StatementPaymentDto> Payments);
