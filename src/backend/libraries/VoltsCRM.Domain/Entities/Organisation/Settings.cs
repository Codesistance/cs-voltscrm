using VoltsCRM.Domain.Common;

namespace VoltsCRM.Domain.Entities.Organisation;

/// <summary>
/// Registry row for a payment gateway provider. One row per gateway, keyed by a unique
/// <see cref="KeyName"/> which must match an implemented adapter's <c>ProviderKey</c>.
/// A gateway is offered to customers only when it is both implemented (a registered adapter)
/// and has a row here with <see cref="Visibility"/> = true.
/// </summary>
public class PaymentGatewayConfig : Entity
{
    /// <summary>Unique provider key; must equal an implemented adapter's <c>ProviderKey</c> (e.g. "voltspayments").</summary>
    public string KeyName { get; private set; } = string.Empty;

    /// <summary>Customer-facing label.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Admin toggle controlling whether the gateway is offered.</summary>
    public bool Visibility { get; private set; }

    /// <summary>Free-form provider configuration/secrets (e.g. webhookSecret). Persisted as jsonb.</summary>
    public Dictionary<string, string> Data { get; private set; } = new();

    private PaymentGatewayConfig() { }

    public PaymentGatewayConfig(string keyName, string displayName, bool visibility, Dictionary<string, string>? data = null)
    {
        KeyName = keyName;
        DisplayName = displayName;
        Visibility = visibility;
        Data = data ?? new();
    }

    /// <summary>
    /// Updates display name, visibility and config data. Data is merged: non-empty values overwrite,
    /// empty values are ignored so secrets stay write-only (a masked read can be PUT back unchanged).
    /// </summary>
    public void Update(string displayName, bool visibility, IReadOnlyDictionary<string, string>? data)
    {
        DisplayName = displayName;
        Visibility = visibility;
        if (data is not null)
        {
            foreach (var (k, v) in data)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    Data[k] = v;
            }
        }
    }

    public void SetVisibility(bool visible) => Visibility = visible;
}

/// <summary>Configuration for auto-debit/recurring payments.</summary>
public class AutoDebitSettings : Entity
{
    public string Provider { get; private set; } = string.Empty;
    public int RetryDays { get; private set; } = 3;
    public bool Enabled { get; private set; }

    private AutoDebitSettings() { }

    public AutoDebitSettings(string provider, int retryDays, bool enabled)
    {
        Provider = provider;
        RetryDays = retryDays;
        Enabled = enabled;
    }

    public void Update(string provider, int retryDays, bool enabled)
    {
        Provider = provider;
        RetryDays = retryDays;
        Enabled = enabled;
    }
}

/// <summary>Configuration for token vending platform integration.</summary>
public class TokenVendingSettings : Entity
{
    public string Provider { get; private set; } = string.Empty;
    public string ApiKey { get; private set; } = string.Empty;
    public bool Active { get; private set; }

    private TokenVendingSettings() { }

    public TokenVendingSettings(string provider, string apiKey, bool active)
    {
        Provider = provider;
        ApiKey = apiKey;
        Active = active;
    }

    public void Update(string provider, string? apiKey, bool active)
    {
        Provider = provider;
        if (!string.IsNullOrWhiteSpace(apiKey))
            ApiKey = apiKey;
        Active = active;
    }
}
