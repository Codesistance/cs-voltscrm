using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.Invoices;

public static class InvoiceMapping
{
    public static InvoiceListItemDto ToListItem(
        this Invoice invoice,
        string customerName,
        string customerAccountNumber) =>
        new(
            invoice.Id,
            invoice.CustomerId,
            invoice.CustomerSubscriptionId,
            customerName,
            customerAccountNumber,
            invoice.PeriodYear,
            invoice.PeriodMonth,
            new MoneyDto(invoice.GrossAmount, invoice.Currency),
            new MoneyDto(invoice.DiscountAmount, invoice.Currency),
            new MoneyDto(invoice.AmountDue, invoice.Currency),
            new MoneyDto(invoice.AmountPaid, invoice.Currency),
            new MoneyDto(invoice.Balance, invoice.Currency),
            invoice.DueDate,
            invoice.Status.ToString());

    public static async Task<InvoiceDto> ToDetailDtoAsync(
        this Invoice invoice,
        IAppDbContext db,
        CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Id == invoice.CustomerId)
            .Select(c => new
            {
                c.AccountNumber,
                c.PersonalInfo.FirstName,
                c.PersonalInfo.LastName,
            })
            .FirstOrDefaultAsync(ct);

        var lineItems = invoice.LineItems
            .Select(item => new InvoiceLineItemDto(item.Id, item.Description, item.Amount, item.IsDiscount))
            .ToList();

        return new InvoiceDto(
            invoice.Id,
            invoice.CustomerId,
            invoice.CustomerSubscriptionId,
            customer is null ? "—" : $"{customer.FirstName} {customer.LastName}",
            customer?.AccountNumber ?? "—",
            invoice.PeriodYear,
            invoice.PeriodMonth,
            new MoneyDto(invoice.GrossAmount, invoice.Currency),
            new MoneyDto(invoice.DiscountAmount, invoice.Currency),
            new MoneyDto(invoice.AmountDue, invoice.Currency),
            new MoneyDto(invoice.AmountPaid, invoice.Currency),
            new MoneyDto(invoice.Balance, invoice.Currency),
            invoice.DueDate,
            invoice.Status.ToString(),
            lineItems,
            invoice.CreatedAt,
            invoice.UpdatedAt);
    }

    /// <summary>Synchronous version when customer data is pre-loaded (avoids N+1).</summary>
    public static InvoiceDto ToDetailDto(
        this Invoice invoice,
        string customerName,
        string customerAccountNumber)
    {
        var lineItems = invoice.LineItems
            .Select(item => new InvoiceLineItemDto(item.Id, item.Description, item.Amount, item.IsDiscount))
            .ToList();

        return new InvoiceDto(
            invoice.Id,
            invoice.CustomerId,
            invoice.CustomerSubscriptionId,
            customerName,
            customerAccountNumber,
            invoice.PeriodYear,
            invoice.PeriodMonth,
            new MoneyDto(invoice.GrossAmount, invoice.Currency),
            new MoneyDto(invoice.DiscountAmount, invoice.Currency),
            new MoneyDto(invoice.AmountDue, invoice.Currency),
            new MoneyDto(invoice.AmountPaid, invoice.Currency),
            new MoneyDto(invoice.Balance, invoice.Currency),
            invoice.DueDate,
            invoice.Status.ToString(),
            lineItems,
            invoice.CreatedAt,
            invoice.UpdatedAt);
    }

    public static PaymentAccountDto ToDto(this PaymentAccount account) =>
        new(
            account.Id,
            account.CustomerId,
            new MoneyDto(account.Balance, account.Currency),
            account.LastPaymentDate,
            account.CreatedAt,
            account.UpdatedAt);
}
