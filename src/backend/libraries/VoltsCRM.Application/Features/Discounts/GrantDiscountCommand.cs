using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Discounts;

public sealed record GrantDiscountCommand(
    Guid CustomerId,
    string DiscountType,
    decimal Value,
    string Scope,
    Guid? TargetId,
    bool IsRecurring,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    string GrantedByUserId,
    string? Reason) : IRequest<DiscountGrantDto>;

public sealed class GrantDiscountCommandValidator : AbstractValidator<GrantDiscountCommand>
{
    public GrantDiscountCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Value).GreaterThan(0m);
        RuleFor(x => x.DiscountType)
            .Must(v => Enum.TryParse<DiscountType>(v, true, out _))
            .WithMessage("Invalid discount type.");
        RuleFor(x => x.Scope)
            .Must(v => Enum.TryParse<DiscountScope>(v, true, out _))
            .WithMessage("Invalid discount scope.");
        RuleFor(x => x.GrantedByUserId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x)
            .Must(x => x.ValidUntil is null || x.ValidFrom is null || x.ValidUntil >= x.ValidFrom)
            .WithMessage("ValidUntil must be after or equal to ValidFrom.");
    }
}

public sealed class GrantDiscountCommandHandler(IAppDbContext db)
    : IRequestHandler<GrantDiscountCommand, DiscountGrantDto>
{
    public async Task<DiscountGrantDto> Handle(GrantDiscountCommand cmd, CancellationToken ct)
    {
        var customerExists = await db.Customers.AsNoTracking().AnyAsync(c => c.Id == cmd.CustomerId, ct);
        if (!customerExists)
            throw new ValidationException("Customer does not exist.");

        var grant = new DiscountGrant(
            cmd.CustomerId,
            Enum.Parse<DiscountType>(cmd.DiscountType, true),
            cmd.Value,
            Enum.Parse<DiscountScope>(cmd.Scope, true),
            cmd.GrantedByUserId,
            cmd.IsRecurring,
            cmd.TargetId,
            cmd.ValidFrom,
            cmd.ValidUntil,
            cmd.Reason);

        db.DiscountGrants.Add(grant);
        await db.SaveChangesAsync(ct);

        // Fetch customer name for the DTO
        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Id == cmd.CustomerId)
            .Select(c => new { c.PersonalInfo.FirstName, c.PersonalInfo.LastName })
            .FirstAsync(ct);
        var customerName = $"{customer.FirstName} {customer.LastName}".Trim();

        return new DiscountGrantDto(
            grant.Id,
            grant.CustomerId,
            customerName,
            grant.DiscountType.ToString(),
            grant.Value,
            grant.Scope.ToString(),
            grant.TargetId,
            grant.IsRecurring,
            grant.ValidFrom,
            grant.ValidUntil,
            grant.GrantedByUserId,
            grant.GrantedAt,
            grant.Reason,
            grant.Status.ToString(),
            grant.CreatedAt,
            grant.UpdatedAt);
    }
}
