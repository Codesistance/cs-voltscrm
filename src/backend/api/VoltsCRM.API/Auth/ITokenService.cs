using VoltsCRM.Infrastructure.Identity;

namespace VoltsCRM.API.Auth;

public interface ITokenService
{
    AccessToken CreateAccessToken(AppUser user, IEnumerable<string> roles, IEnumerable<string> permissions);
    RefreshTokenPair CreateRefreshToken();
    string Hash(string token);
}

public readonly record struct AccessToken(string Value, DateTimeOffset ExpiresAt, int ExpiresInSeconds);

public readonly record struct RefreshTokenPair(string Value, string Hash, DateTimeOffset ExpiresAt);
