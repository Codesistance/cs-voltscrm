namespace VoltsCRM.API.Auth;

/// <summary>Generates a random password that satisfies the Identity password policy.</summary>
public static class PasswordGenerator
{
    public static string GenerateTemporary()
        => $"{Guid.NewGuid():N}"[..12] + "Aa1!";
}
