namespace VoltsCRM.API.Auth;

/// <summary>Custom JWT claim types issued and authorized on by VoltsCRM.</summary>
public static class AppClaims
{
    /// <summary>The user's <c>UserType</c> (Customer/Agent/Administration). Drives UI area + type policies.</summary>
    public const string UserType = "user_type";

    /// <summary>One claim per granted permission key (Administration users only), e.g. <c>perm: invoices.view</c>.</summary>
    public const string Permission = "perm";
}
