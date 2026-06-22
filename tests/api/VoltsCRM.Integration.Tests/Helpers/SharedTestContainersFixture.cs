using Testcontainers.PostgreSql;

namespace VoltsCRM.Integration.Tests.Helpers;

/// <summary>
/// Starts one PostgreSQL container for the whole test assembly. Each test creates its own isolated
/// database inside it (see <see cref="TestDatabaseHelper"/>) for isolation without per-test container
/// startup cost. Mirrors Concairge's <c>SharedTestContainersFixture</c> (Postgres only).
/// </summary>
public class SharedTestContainersFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgreSqlContainer;

    public PostgreSqlContainer PostgreSqlContainer => _postgreSqlContainer
        ?? throw new InvalidOperationException("PostgreSQL container not initialized. Call InitializeAsync first.");

    public string PostgresConnectionString => PostgreSqlContainer.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        _postgreSqlContainer = PostgresTestContainerFactory.CreateStandardContainer();
        await _postgreSqlContainer.StartAsync();
        await PostgresTestContainerFactory.WaitForPostgresReadyAsync(PostgresConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        if (_postgreSqlContainer != null)
            await _postgreSqlContainer.DisposeAsync();
    }
}

/// <summary>Shares a single container instance across every test in the collection.</summary>
[CollectionDefinition("SharedTestContainers")]
public class SharedTestContainersCollection : ICollectionFixture<SharedTestContainersFixture>;
