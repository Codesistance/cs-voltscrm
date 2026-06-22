using Microsoft.EntityFrameworkCore;
using Npgsql;
using VoltsCRM.Infrastructure.Persistence;

namespace VoltsCRM.Integration.Tests.Helpers;

/// <summary>
/// Creates and manages an isolated database per test inside the shared PostgreSQL container, applies
/// EF Core migrations, and drops it on dispose. Adapted from Concairge's <c>TestDatabaseHelper</c>
/// (no pgvector extension; VoltsCRM migrations live in the Infrastructure assembly by default).
/// </summary>
public sealed class TestDatabaseHelper(string masterConnectionString, string? testName = null) : IAsyncDisposable
{
    private readonly string _databaseName = BuildDatabaseName(testName);
    private string? _testConnectionString;

    public string ConnectionString => _testConnectionString
        ?? throw new InvalidOperationException("Database not created. Call CreateDatabaseAsync first.");

    /// <summary>Creates the isolated database and applies all migrations (schema + seeded admin user).</summary>
    public async Task CreateDatabaseAsync()
    {
        await using (var connection = new NpgsqlConnection(masterConnectionString))
        {
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", connection);
            await cmd.ExecuteNonQueryAsync();
        }

        _testConnectionString = new NpgsqlConnectionStringBuilder(masterConnectionString)
        {
            Database = _databaseName,
        }.ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_testConnectionString)
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_testConnectionString is null) return;

        try
        {
            await using var connection = new NpgsqlConnection(masterConnectionString);
            await connection.OpenAsync();

            await using (var terminate = new NpgsqlCommand(
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid()",
                connection))
            {
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\"", connection);
            await drop.ExecuteNonQueryAsync();
        }
        catch
        {
            // Cleanup is best-effort — the container is destroyed at the end of the run anyway.
        }
    }

    private static string BuildDatabaseName(string? testName)
    {
        var sanitized = new string([.. (testName ?? "test")
            .Where(c => char.IsLetterOrDigit(c) || c == '_')
            .Take(30)]).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(sanitized)) sanitized = "test";
        return $"test_{sanitized}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }
}
