using Amazon.SimpleEmailV2;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Options;
using VoltsCRM.Infrastructure.Authorization;
using VoltsCRM.Infrastructure.Email;
using VoltsCRM.Infrastructure.Geocoding;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Infrastructure.Integrations;
using VoltsCRM.Infrastructure.Persistence;
using VoltsCRM.Infrastructure.Persistence.Interceptors;

namespace VoltsCRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public");
                npgsql.CommandTimeout(30);
                npgsql.EnableRetryOnFailure(3);
            });

            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        // Expose the context behind the Application abstraction for MediatR handlers.
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IPermissionResolver, PermissionResolver>();

        // Email: use real SES when a sender address is configured, otherwise log the message (dev).
        var emailFrom = configuration[$"{EmailOptions.SectionName}:{nameof(EmailOptions.FromAddress)}"];
        if (string.IsNullOrWhiteSpace(emailFrom))
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            services.AddSingleton<IAmazonSimpleEmailServiceV2>(_ => new AmazonSimpleEmailServiceV2Client());
            services.AddSingleton<IEmailSender, SesEmailSender>();
        }

        services.AddIdentityCore<AppUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>();
        // AddDefaultTokenProviders() is added in the API layer (requires ASP.NET Core Identity)

        // Geocoding (address -> coordinates), proxied through our backend with the required
        // User-Agent and result caching. See GeocodingOptions for the OSM/Nominatim usage policy.
        services.AddMemoryCache();
        services.Configure<GeocodingOptions>(configuration.GetSection(GeocodingOptions.SectionName));
        var geocoding = configuration.GetSection(GeocodingOptions.SectionName).Get<GeocodingOptions>() ?? new GeocodingOptions();
        services.AddHttpClient<IGeocoder, NominatimGeocoder>(client =>
        {
            client.BaseAddress = new Uri(geocoding.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(geocoding.UserAgent);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Payment gateways (keyed services by provider name). The implemented-key set below is the
        // source of truth for IPaymentGatewayCatalog (keyed DI cannot enumerate its own keys).
        services.AddKeyedScoped<IPaymentGateway, VoltspaymentsGateway>(VoltspaymentsGateway.Key);
        services.AddKeyedScoped<IPaymentGateway, MpesaPaymentGateway>("mpesa");
        services.AddKeyedScoped<IPaymentGateway, StripePaymentGateway>("stripe");

        IReadOnlyCollection<string> implementedGatewayKeys = [VoltspaymentsGateway.Key, "mpesa", "stripe"];
        services.AddScoped<IPaymentGatewayCatalog>(sp => new PaymentGatewayCatalog(sp, implementedGatewayKeys));

        // Token vending platforms (keyed services by provider name)
        services.AddKeyedScoped<ITokenVendingPlatform, KplcTokenVendingPlatform>("kplc");

        return services;
    }
}
