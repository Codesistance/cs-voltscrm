namespace VoltsCRM.API.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    // When true the refresh token is returned in the response body and replayed in the
    // request body instead of an httpOnly cookie. Used when the SPA and API live on
    // different origins over plain HTTP (no custom domain), where a Secure SameSite=Strict
    // cookie can't be carried. The cookie path stays the default for the custom-domain edge.
    public bool RefreshTokenInBody { get; set; }
}
