namespace VoltsCRM.Application.Common.Options;

/// <summary>
/// Email/notification configuration. Bind from the "Email" section; override per-environment via
/// <c>Email__FromAddress</c> / <c>Email__AppBaseUrl</c>. When <see cref="FromAddress"/> is empty the
/// app falls back to a logging email sender (no AWS SES call), which is convenient for local dev.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Verified SES sender address. Empty ⇒ use the logging email sender.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Public base URL of the SPA, used to build links (e.g. the set-password invite link).</summary>
    public string AppBaseUrl { get; set; } = "http://localhost:5173";
}
