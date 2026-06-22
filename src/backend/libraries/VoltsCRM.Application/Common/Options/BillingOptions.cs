using VoltsCRM.Domain.Common;

namespace VoltsCRM.Application.Common.Options;

/// <summary>
/// Billing configuration. Bind from the "Billing" section; override per-environment via the
/// <c>Billing__DefaultCurrency</c> environment variable.
/// </summary>
public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    /// <summary>ISO 4217 currency code used as the default across billing (invoices, payments, etc.).</summary>
    public string DefaultCurrency { get; set; } = Money.DefaultCurrency;
}
