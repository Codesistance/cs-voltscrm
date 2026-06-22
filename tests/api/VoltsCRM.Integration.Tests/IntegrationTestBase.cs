using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests;

/// <summary>
/// Base for container-backed API tests. Each test class gets its own isolated, migrated + seeded
/// database inside the shared PostgreSQL container, and a booted API client.
/// </summary>
public abstract class IntegrationTestBase(SharedTestContainersFixture fixture) : IAsyncLifetime
{
    protected CustomWebApplicationFactory Factory = null!;
    protected HttpClient Client = null!;

    /// <summary>Used to name the isolated database for easier debugging.</summary>
    protected abstract string TestName { get; }

    public async ValueTask InitializeAsync()
    {
        Factory = new CustomWebApplicationFactory(fixture.PostgresConnectionString, TestName);
        await Factory.InitializeAsync();
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        if (Factory is not null)
            await Factory.DisposeAsync();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? token = null, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return Client.SendAsync(request, Ct);
    }

    protected Task<HttpResponseMessage> GetAsync(string url, string? token = null) => SendAsync(HttpMethod.Get, url, token);
    protected Task<HttpResponseMessage> PostAsync(string url, object? body, string? token = null) => SendAsync(HttpMethod.Post, url, token, body);
    protected Task<HttpResponseMessage> PutAsync(string url, object? body, string? token = null) => SendAsync(HttpMethod.Put, url, token, body);
    protected Task<HttpResponseMessage> DeleteAsync(string url, string? token = null) => SendAsync(HttpMethod.Delete, url, token);

    protected static Task<T?> ReadAsync<T>(HttpResponseMessage response) => response.Content.ReadFromJsonAsync<T>(Ct);
}
