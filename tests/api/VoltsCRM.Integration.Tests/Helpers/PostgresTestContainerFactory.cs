using Npgsql;
using Testcontainers.PostgreSql;

namespace VoltsCRM.Integration.Tests.Helpers;

/// <summary>
/// Builds the shared PostgreSQL test container. Adapted from Concairge's
/// <c>PostgresTestContainerFactory</c> — plain Postgres (VoltsCRM uses no pgvector). The PostgreSql
/// module ships a built-in readiness wait; <see cref="WaitForPostgresReadyAsync"/> adds a belt-and-
/// suspenders check before we open pooled connections.
/// </summary>
public static class PostgresTestContainerFactory
{
    public static PostgreSqlContainer CreateStandardContainer()
    {
        return new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("voltscrm_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();
    }

    /// <summary>Waits for PostgreSQL to accept connections, with exponential backoff.</summary>
    public static async Task<bool> WaitForPostgresReadyAsync(string connectionString, int maxRetries = 10, int baseDelayMs = 300)
    {
        var delay = TimeSpan.FromMilliseconds(baseDelayMs);

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                await connection.CloseAsync();
                return true;
            }
            catch
            {
                if (i == maxRetries - 1)
                    return false;
                await Task.Delay(delay);
                delay *= 2;
            }
        }

        return false;
    }
}
