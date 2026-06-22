using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Organisation;

namespace VoltsCRM.Application.Features.Settings;

// DTOs - secrets are masked on read
/// <summary>A payment gateway registry row. <see cref="Implemented"/> reflects whether an adapter is
/// registered for <see cref="KeyName"/>; secret-ish values in <see cref="Data"/> are masked.</summary>
public sealed record PaymentGatewayConfigDto(
    string KeyName, string DisplayName, bool Visibility, bool Implemented, IReadOnlyDictionary<string, string> Data);
public sealed record AutoDebitSettingsDto(string Provider, int RetryDays, bool Enabled);
public sealed record TokenVendingSettingsDto(string Provider, string ApiKey, bool Active);

// Input records for updates
public sealed record UpsertPaymentGatewayConfigInput(string DisplayName, bool Visibility, Dictionary<string, string>? Data);
public sealed record UpdateAutoDebitSettingsInput(string Provider, int RetryDays, bool Enabled);
public sealed record UpdateTokenVendingSettingsInput(string Provider, string? ApiKey, bool Active);

// Queries
public sealed record ListPaymentGatewayConfigsQuery : IRequest<IReadOnlyList<PaymentGatewayConfigDto>>;
public sealed record GetAutoDebitSettingsQuery : IRequest<AutoDebitSettingsDto?>;
public sealed record GetTokenVendingSettingsQuery : IRequest<TokenVendingSettingsDto?>;

// Commands
public sealed record UpsertPaymentGatewayConfigCommand(string KeyName, UpsertPaymentGatewayConfigInput Input) : IRequest<PaymentGatewayConfigDto>;
public sealed record SetPaymentGatewayVisibilityCommand(string KeyName, bool Visible) : IRequest<PaymentGatewayConfigDto>;
public sealed record UpdateAutoDebitSettingsCommand(UpdateAutoDebitSettingsInput Input) : IRequest<AutoDebitSettingsDto>;
public sealed record UpdateTokenVendingSettingsCommand(UpdateTokenVendingSettingsInput Input) : IRequest<TokenVendingSettingsDto>;

/// <summary>Masks secret-ish config values on read and strips masked sentinels on write so secrets stay write-only.</summary>
public static class GatewaySecretMasking
{
    public const string Mask = "••••••••";
    private static readonly string[] SecretHints = ["secret", "key", "password", "token", "apikey"];

    public static bool IsSecret(string name) =>
        SecretHints.Any(h => name.Contains(h, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyDictionary<string, string> MaskForRead(IReadOnlyDictionary<string, string> data) =>
        data.ToDictionary(
            kv => kv.Key,
            kv => IsSecret(kv.Key) && !string.IsNullOrEmpty(kv.Value) ? Mask : kv.Value);

    /// <summary>Drops entries whose value is the mask sentinel (a masked read PUT back unchanged) so they don't overwrite stored secrets.</summary>
    public static Dictionary<string, string> StripMasked(Dictionary<string, string>? data) =>
        (data ?? new()).Where(kv => kv.Value != Mask).ToDictionary(kv => kv.Key, kv => kv.Value);
}

// Handlers
public sealed class ListPaymentGatewayConfigsHandler(IAppDbContext db, IPaymentGatewayCatalog catalog)
    : IRequestHandler<ListPaymentGatewayConfigsQuery, IReadOnlyList<PaymentGatewayConfigDto>>
{
    public async Task<IReadOnlyList<PaymentGatewayConfigDto>> Handle(ListPaymentGatewayConfigsQuery query, CancellationToken ct)
    {
        var rows = await db.PaymentGatewayConfigs.AsNoTracking().ToListAsync(ct);

        var result = rows
            .Select(r => new PaymentGatewayConfigDto(
                r.KeyName, r.DisplayName, r.Visibility,
                catalog.ImplementedKeys.Contains(r.KeyName),
                GatewaySecretMasking.MaskForRead(r.Data)))
            .ToList();

        // Surface implemented adapters that have no config row yet, so an admin can enable them.
        var configured = rows.Select(r => r.KeyName).ToHashSet();
        foreach (var key in catalog.ImplementedKeys.Where(k => !configured.Contains(k)))
            result.Add(new PaymentGatewayConfigDto(key, key, false, true, new Dictionary<string, string>()));

        return result;
    }
}

public sealed class UpsertPaymentGatewayConfigHandler(IAppDbContext db, IPaymentGatewayCatalog catalog)
    : IRequestHandler<UpsertPaymentGatewayConfigCommand, PaymentGatewayConfigDto>
{
    public async Task<PaymentGatewayConfigDto> Handle(UpsertPaymentGatewayConfigCommand cmd, CancellationToken ct)
    {
        // A gateway can only be made visible if an adapter is actually implemented for the key.
        if (cmd.Input.Visibility && !catalog.ImplementedKeys.Contains(cmd.KeyName))
            throw new ValidationException([new ValidationFailure(nameof(cmd.KeyName),
                $"No payment gateway adapter is implemented for '{cmd.KeyName}'; it cannot be made visible.")]);

        var cleanData = GatewaySecretMasking.StripMasked(cmd.Input.Data);

        var config = await db.PaymentGatewayConfigs.FirstOrDefaultAsync(c => c.KeyName == cmd.KeyName, ct);
        if (config is null)
        {
            config = new PaymentGatewayConfig(cmd.KeyName, cmd.Input.DisplayName, cmd.Input.Visibility, cleanData);
            db.PaymentGatewayConfigs.Add(config);
        }
        else
        {
            config.Update(cmd.Input.DisplayName, cmd.Input.Visibility, cleanData);
        }

        await db.SaveChangesAsync(ct);
        return ToDto(config, catalog);
    }

    internal static PaymentGatewayConfigDto ToDto(PaymentGatewayConfig c, IPaymentGatewayCatalog catalog) =>
        new(c.KeyName, c.DisplayName, c.Visibility,
            catalog.ImplementedKeys.Contains(c.KeyName),
            GatewaySecretMasking.MaskForRead(c.Data));
}

public sealed class SetPaymentGatewayVisibilityHandler(IAppDbContext db, IPaymentGatewayCatalog catalog)
    : IRequestHandler<SetPaymentGatewayVisibilityCommand, PaymentGatewayConfigDto>
{
    public async Task<PaymentGatewayConfigDto> Handle(SetPaymentGatewayVisibilityCommand cmd, CancellationToken ct)
    {
        var config = await db.PaymentGatewayConfigs.FirstOrDefaultAsync(c => c.KeyName == cmd.KeyName, ct)
            ?? throw new ValidationException([new ValidationFailure(nameof(cmd.KeyName),
                $"No payment gateway config exists for '{cmd.KeyName}'.")]);

        if (cmd.Visible && !catalog.ImplementedKeys.Contains(cmd.KeyName))
            throw new ValidationException([new ValidationFailure(nameof(cmd.KeyName),
                $"No payment gateway adapter is implemented for '{cmd.KeyName}'; it cannot be made visible.")]);

        config.SetVisibility(cmd.Visible);
        await db.SaveChangesAsync(ct);
        return UpsertPaymentGatewayConfigHandler.ToDto(config, catalog);
    }
}

public sealed class GetAutoDebitSettingsHandler(IAppDbContext db) : IRequestHandler<GetAutoDebitSettingsQuery, AutoDebitSettingsDto?>
{
    public async Task<AutoDebitSettingsDto?> Handle(GetAutoDebitSettingsQuery query, CancellationToken ct)
    {
        var settings = await db.AutoDebitSettings.FirstOrDefaultAsync(ct);
        if (settings is null) return null;

        return new AutoDebitSettingsDto(settings.Provider, settings.RetryDays, settings.Enabled);
    }
}

public sealed class UpdateAutoDebitSettingsHandler(IAppDbContext db) : IRequestHandler<UpdateAutoDebitSettingsCommand, AutoDebitSettingsDto>
{
    public async Task<AutoDebitSettingsDto> Handle(UpdateAutoDebitSettingsCommand cmd, CancellationToken ct)
    {
        var settings = await db.AutoDebitSettings.FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            settings = new AutoDebitSettings(cmd.Input.Provider, cmd.Input.RetryDays, cmd.Input.Enabled);
            db.AutoDebitSettings.Add(settings);
        }
        else
        {
            settings.Update(cmd.Input.Provider, cmd.Input.RetryDays, cmd.Input.Enabled);
        }

        await db.SaveChangesAsync(ct);

        return new AutoDebitSettingsDto(settings.Provider, settings.RetryDays, settings.Enabled);
    }
}

public sealed class GetTokenVendingSettingsHandler(IAppDbContext db) : IRequestHandler<GetTokenVendingSettingsQuery, TokenVendingSettingsDto?>
{
    public async Task<TokenVendingSettingsDto?> Handle(GetTokenVendingSettingsQuery query, CancellationToken ct)
    {
        var settings = await db.TokenVendingSettings.FirstOrDefaultAsync(ct);
        if (settings is null) return null;

        // Mask the secret on read
        var maskedKey = string.IsNullOrEmpty(settings.ApiKey) ? "" : "••••••••";
        return new TokenVendingSettingsDto(settings.Provider, maskedKey, settings.Active);
    }
}

public sealed class UpdateTokenVendingSettingsHandler(IAppDbContext db) : IRequestHandler<UpdateTokenVendingSettingsCommand, TokenVendingSettingsDto>
{
    public async Task<TokenVendingSettingsDto> Handle(UpdateTokenVendingSettingsCommand cmd, CancellationToken ct)
    {
        var settings = await db.TokenVendingSettings.FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            settings = new TokenVendingSettings(cmd.Input.Provider, cmd.Input.ApiKey ?? "", cmd.Input.Active);
            db.TokenVendingSettings.Add(settings);
        }
        else
        {
            settings.Update(cmd.Input.Provider, cmd.Input.ApiKey, cmd.Input.Active);
        }

        await db.SaveChangesAsync(ct);

        var maskedKey = string.IsNullOrEmpty(settings.ApiKey) ? "" : "••••••••";
        return new TokenVendingSettingsDto(settings.Provider, maskedKey, settings.Active);
    }
}
