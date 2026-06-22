using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VoltsCRM.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build <see cref="AppDbContext"/> without booting the
/// API host (which requires runtime secrets such as Jwt:Key). Reads the connection string from the
/// <c>ConnectionStrings__DefaultConnection</c> environment variable, falling back to the local dev DB.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=voltscrm;Username=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .Options;

        return new AppDbContext(options);
    }
}
