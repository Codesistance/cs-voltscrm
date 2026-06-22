using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VoltsCRM.API.Auth;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Integration.Tests.Helpers;

/// <summary>
/// Mints JWTs signed with the test signing key, with the same claim shape as the production
/// <c>JwtTokenService</c> (sub / email / name / user_type / perm). Lets authorization tests assert
/// policy behaviour without driving the full login flow. Tokens validate against the real pipeline.
/// </summary>
public static class TestTokenFactory
{
    public static string Create(
        string userId,
        UserType userType,
        IEnumerable<string>? permissions = null,
        string email = "test@voltscrm.local",
        string fullName = "Test User")
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("name", fullName),
            new(AppClaims.UserType, userType.ToString()),
        };
        claims.AddRange((permissions ?? []).Select(p => new Claim(AppClaims.Permission, p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CustomWebApplicationFactory.TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: CustomWebApplicationFactory.JwtIssuer,
            audience: CustomWebApplicationFactory.JwtAudience,
            claims: claims,
            notBefore: now,
            expires: now.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
