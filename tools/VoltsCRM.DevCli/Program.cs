using System.Globalization;
using VoltsCRM.Infrastructure.Identity;

// VoltsCRM dev CLI — prints the seeded admin credentials.
//
// Usage:
//   dotnet run --project tools/VoltsCRM.DevCli                 (today, UTC)
//   dotnet run --project tools/VoltsCRM.DevCli -- --date 16062026
//
// The password rotates daily; it's computed by the same generator the app uses,
// so what this prints always matches what the seeded admin actually logs in with.
// Requires the secret HMAC key in the Seed__HmacKey env var (the value stored in AWS Secrets
// Manager, or your local dev key) — without it this prints nothing, by design.

var date = DateTime.UtcNow;

var dateArgIndex = Array.IndexOf(args, "--date");
if (dateArgIndex >= 0 && dateArgIndex + 1 < args.Length)
{
    if (!DateTime.TryParseExact(
            args[dateArgIndex + 1], "ddMMyyyy",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
    {
        Console.WriteLine("Invalid date format. Use ddMMyyyy (e.g., 16062026)");
        return 1;
    }
}

var key = Environment.GetEnvironmentVariable("Seed__HmacKey");
if (string.IsNullOrEmpty(key))
{
    Console.WriteLine("Seed__HmacKey environment variable is not set.");
    Console.WriteLine("Export the seed HMAC key (your local dev key, or the value from AWS Secrets");
    Console.WriteLine("Manager: voltscrm-<env>/seed-hmac-key) and re-run.");
    return 1;
}

var password = SeedCredentialGenerator.ComputePassword(date, key);

Console.WriteLine();
Console.WriteLine("Seeded admin credentials");
Console.WriteLine($"  Date:     {date:dd/MM/yyyy} (UTC)");
Console.WriteLine($"  Email:    {SeedCredentialGenerator.SeededAdminEmail}");
Console.WriteLine($"  Password: {password}");
Console.WriteLine();

return 0;
