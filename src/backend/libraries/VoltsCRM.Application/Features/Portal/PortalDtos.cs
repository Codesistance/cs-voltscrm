using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.Portal;

public sealed record PortalSummaryDto(
    MoneyDto OutstandingBalance,
    int PendingInvoices,
    int ActiveSubscriptions,
    MoneyDto PaidThisMonth);

public sealed record PortalInvoiceDto(
    Guid Id,
    int PeriodYear,
    int PeriodMonth,
    MoneyDto AmountDue,
    MoneyDto AmountPaid,
    MoneyDto Balance,
    DateTimeOffset DueDate,
    string Status);

public sealed record PortalSubscriptionDto(
    Guid Id,
    Guid ServicePlanId,
    string PlanName,
    string Status,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    MoneyDto EffectivePrice);

public sealed record PortalPaymentDto(
    Guid Id,
    MoneyDto Amount,
    MoneyDto NetAmount,
    string Method,
    string Channel,
    string Status,
    DateTimeOffset PaymentDate,
    string? PlatformReference);

public sealed record PortalProfileDto(
    string AccountNumber,
    string FullName,
    string Phone,
    string? Email,
    string Status,
    AddressDto Address);
