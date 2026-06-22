namespace VoltsCRM.Application.Common.Models;

/// <summary>Money normalised for transport: amount + ISO currency code.</summary>
public sealed record MoneyDto(decimal Amount, string Currency);
