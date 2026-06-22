using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Crm;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Reports;

public sealed record CustomerStatementQuery(
    Guid CustomerId,
    DateOnly? From,
    DateOnly? To) : IRequest<CustomerStatementDto>;

public sealed class CustomerStatementQueryValidator : AbstractValidator<CustomerStatementQuery>
{
    public CustomerStatementQueryValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x).Must(x => x.To is null || x.From is null || x.To >= x.From)
            .WithMessage("To date must be on or after From date.");
    }
}

public sealed class CustomerStatementQueryHandler(IAppDbContext db)
    : IRequestHandler<CustomerStatementQuery, CustomerStatementDto>
{
    public async Task<CustomerStatementDto> Handle(CustomerStatementQuery query, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Id == query.CustomerId)
            .Select(c => new { c.AccountNumber, c.PersonalInfo.FirstName, c.PersonalInfo.LastName })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Customer), query.CustomerId);

        var from = query.From?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusive = query.To?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var invoicesQuery = db.Invoices.AsNoTracking().Where(i => i.CustomerId == query.CustomerId);
        if (from is { } fromDate)
            invoicesQuery = invoicesQuery.Where(i => i.DueDate >= fromDate);
        if (toExclusive is { } toDate)
            invoicesQuery = invoicesQuery.Where(i => i.DueDate < toDate);

        var paymentsQuery = db.Payments.AsNoTracking().Where(p => p.CustomerId == query.CustomerId);
        if (from is { } fromPaymentDate)
            paymentsQuery = paymentsQuery.Where(p => p.PaymentDate >= fromPaymentDate);
        if (toExclusive is { } toPaymentDate)
            paymentsQuery = paymentsQuery.Where(p => p.PaymentDate < toPaymentDate);

        var invoices = await invoicesQuery
            .OrderByDescending(i => i.DueDate)
            .ToListAsync(ct);
        var payments = await paymentsQuery
            .Where(p => p.Status == PaymentStatus.Completed)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(ct);

        var currency = invoices.FirstOrDefault()?.Currency
                       ?? payments.FirstOrDefault()?.Currency
                       ?? Money.DefaultCurrency;

        var invoiceDtos = invoices
            .Select(i => new StatementInvoiceDto(
                i.Id,
                i.DueDate,
                new MoneyDto(i.AmountDue, i.Currency),
                new MoneyDto(i.AmountPaid, i.Currency),
                new MoneyDto(i.Balance, i.Currency),
                i.Status.ToString()))
            .ToList();

        var paymentDtos = payments
            .Select(p => new StatementPaymentDto(
                p.Id,
                p.PaymentDate,
                new MoneyDto(p.Amount, p.Currency),
                new MoneyDto(p.NetAmount, p.Currency),
                p.Method.ToString(),
                p.Status.ToString(),
                p.PlatformReference))
            .ToList();

        return new CustomerStatementDto(
            query.CustomerId,
            $"{customer.FirstName} {customer.LastName}",
            customer.AccountNumber,
            query.From,
            query.To,
            new MoneyDto(invoices.Sum(i => i.AmountDue), currency),
            new MoneyDto(payments.Sum(p => p.NetAmount), currency),
            new MoneyDto(invoices.Sum(i => i.Balance), currency),
            invoiceDtos,
            paymentDtos);
    }
}
