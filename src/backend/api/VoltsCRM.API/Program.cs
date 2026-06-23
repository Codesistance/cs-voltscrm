using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using VoltsCRM.API.Auth;
using VoltsCRM.API.Auth.Permissions;
using VoltsCRM.API.Endpoints;
using VoltsCRM.API.Setup;
using VoltsCRM.Application;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Options;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Infrastructure;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// CLI command: seed-password [--date ddMMyyyy]
// Computes and displays the seeded admin password for a given date (defaults to today).
// Runs after the builder so it can read the secret HMAC key from configuration (Seed:HmacKey).
// Without that secret it prints nothing — it is not a standalone, source-derivable oracle.
if (args.Length > 0 && args[0] == "seed-password")
{
    var date = DateTime.UtcNow;
    var dateArgIndex = Array.IndexOf(args, "--date");
    if (dateArgIndex >= 0 && dateArgIndex + 1 < args.Length)
    {
        if (DateTime.TryParseExact(args[dateArgIndex + 1], "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            date = parsedDate;
        }
        else
        {
            Console.WriteLine("Invalid date format. Use ddMMyyyy (e.g., 25062025)");
            return;
        }
    }

    var seedKeyForCli = builder.Configuration["Seed:HmacKey"];
    if (string.IsNullOrEmpty(seedKeyForCli))
    {
        Console.WriteLine("Seed:HmacKey is not configured. Set it via user-secrets, the Seed__HmacKey env var, or AWS SSM Parameter Store.");
        return;
    }

    var password = SeedCredentialGenerator.ComputePassword(date, seedKeyForCli);
    Console.WriteLine();
    Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║           SEEDED ADMIN PASSWORD GENERATOR                     ║");
    Console.WriteLine("╠═══════════════════════════════════════════════════════════════╣");
    Console.WriteLine($"║  Date:     {date:dd/MM/yyyy}                                        ║");
    Console.WriteLine($"║  Email:    {SeedCredentialGenerator.SeededAdminEmail}                      ║");
    Console.WriteLine($"║  Password: {password}                             ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    return;
}

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console());

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection(BillingOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

// Complete Identity setup (token providers live in ASP.NET Core layer)
builder.Services
    .AddIdentityCore<AppUser>()
    .AddDefaultTokenProviders();

// JWT authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing — set it via user-secrets or AWS SSM Parameter Store.");
if (jwtKey.Length < 32)
    throw new InvalidOperationException($"Jwt:Key must be at least 32 characters (got {jwtKey.Length}). Use user-secrets or SSM Parameter Store.");

// Seed HMAC key — the secret that makes the seeded admin's daily password unguessable. Fail-closed
// (like Jwt:Key): the seeded admin must never fall back to a source-derivable credential.
var seedHmacKey = builder.Configuration["Seed:HmacKey"];
if (string.IsNullOrEmpty(seedHmacKey) || seedHmacKey.Length < 32)
    throw new InvalidOperationException("Seed:HmacKey is missing or shorter than 32 characters — set it via user-secrets, the Seed__HmacKey env var, or AWS SSM Parameter Store.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "VoltsCRM";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "VoltsCRM";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(5)
        };
    });

builder.Services.AddAuthorization(options =>
{
    // One policy per user type ("type:Administration", etc.) for coarse area gating.
    foreach (var type in Enum.GetNames<UserType>())
        options.AddPolicy($"{UserTypePolicy.Prefix}{type}", p => p.RequireClaim(AppClaims.UserType, type));
});

// Fine-grained, dynamic "perm:<key>" policies for Administration users.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(origins)
              .WithHeaders("Authorization", "Content-Type")
              .WithMethods("GET", "POST", "PUT", "DELETE");
    });
});

// Configurable so integration tests can raise the limit and avoid tripping it on /auth.
var authPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Auth:PermitLimit") ?? 10;
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = authPermitLimit;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });

    // Inbound payment webhooks are anonymous (signature-authenticated); cap per IP to blunt abuse.
    var webhookPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Webhook:PermitLimit") ?? 60;
    opts.AddPolicy("webhook", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = webhookPermitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
        }));

    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// CLI command: migrate — apply pending EF Core migrations + seed, then exit.
// Invoked as a one-off ECS task during deploy (RDS is private, so migrations
// must run from inside the VPC; this avoids a migrate-on-boot race across replicas).
if (args.Length > 0 && args[0] == "migrate")
{
    await app.MigrateAndSeedAsync();
    return;
}

// CLI command: reset-db — DROP the database, re-apply migrations, reseed, then exit.
// Destructive: refuses to run in Production unless --force is passed.
if (args.Length > 0 && args[0] == "reset-db")
{
    if (app.Environment.IsProduction() && !args.Contains("--force"))
    {
        Console.WriteLine("Refusing to reset-db in Production without --force.");
        return;
    }
    await app.ReinitializeAndSeedAsync();
    Console.WriteLine("Database reinitialized and seeded.");
    return;
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapApiEndpoints();

if (app.Environment.IsDevelopment())
{
    await app.MigrateAndSeedAsync();
}

// Fail-closed gateway guard: a payment gateway marked visible must be backed by a real adapter.
// A stub adapter (always-true webhook validation) can never be exposed (mirrors the Seed:HmacKey check).
await using (var scope = app.Services.CreateAsyncScope())
{
    var catalog = scope.ServiceProvider.GetRequiredService<IPaymentGatewayCatalog>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var visibleKeys = await db.PaymentGatewayConfigs
        .Where(c => c.Visibility)
        .Select(c => c.KeyName)
        .ToListAsync();

    foreach (var key in visibleKeys)
    {
        var gateway = catalog.Resolve(key);
        if (gateway is null)
            throw new InvalidOperationException($"Payment gateway '{key}' is marked visible but no adapter is implemented for it.");
        if (gateway is IStubGateway)
            throw new InvalidOperationException($"Payment gateway '{key}' is marked visible but is backed by a stub adapter. A stub must never be exposed.");
    }
}

app.Run();

public partial class Program { }
