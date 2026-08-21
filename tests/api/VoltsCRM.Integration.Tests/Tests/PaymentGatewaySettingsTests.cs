using System.Net;
using System.Net.Http.Json;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>
/// Admin payment-gateway registry: lists configs (with an Implemented flag), upserts display name /
/// visibility / data with write-only secret masking, and toggles visibility. Enforces SettingsManage.
/// </summary>
[Collection("SharedTestContainers")]
public class PaymentGatewaySettingsTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(PaymentGatewaySettingsTests);

    private const string Endpoint = "/api/settings/payment-gateways";
    private const string Mask = "••••••••";

    private static string Admin() =>
        TestTokenFactory.Create(Guid.NewGuid().ToString(), UserType.Administration, ["settings.manage"]);

    [Fact]
    public async Task List_WithoutPermission_ReturnsForbidden()
    {
        var token = TestTokenFactory.Create(Guid.NewGuid().ToString(), UserType.Administration);
        var response = await GetAsync(Endpoint, token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_IncludesSeededVoltspaymentsAsImplemented()
    {
        var response = await GetAsync(Endpoint, Admin());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var configs = await response.Content.ReadFromJsonAsync<List<ConfigDtoT>>(TestContext.Current.CancellationToken);
        Assert.NotNull(configs);
        var vp = Assert.Single(configs, c => c.KeyName == "voltspayments");
        Assert.True(vp.Implemented);
        Assert.True(vp.Visibility);
    }

    [Fact]
    public async Task Upsert_SecretInData_IsMaskedOnRead()
    {
        var token = Admin();

        var put = await PutAsync($"{Endpoint}/voltspayments",
            new { displayName = "Volts Payments", visibility = true, data = new { apiSecret = "super-secret-value" } },
            token);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var dto = await put.Content.ReadFromJsonAsync<ConfigDtoT>(TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(Mask, dto.Data["apiSecret"]); // masked on the write response

        // And masked on subsequent reads.
        var list = await (await GetAsync(Endpoint, token)).Content.ReadFromJsonAsync<List<ConfigDtoT>>(TestContext.Current.CancellationToken);
        var vp = Assert.Single(list!, c => c.KeyName == "voltspayments");
        Assert.Equal(Mask, vp.Data["apiSecret"]);
    }

    [Fact]
    public async Task Upsert_MaskedSentinel_DoesNotOverwriteStoredSecret()
    {
        var token = Admin();

        await PutAsync($"{Endpoint}/voltspayments",
            new { displayName = "Volts Payments", visibility = true, data = new { apiSecret = "original-secret" } }, token);

        // PUT the masked value back (as a UI round-trip would) along with a display-name change.
        var put = await PutAsync($"{Endpoint}/voltspayments",
            new { displayName = "Renamed", visibility = true, data = new { apiSecret = Mask } }, token);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var dto = await put.Content.ReadFromJsonAsync<ConfigDtoT>(TestContext.Current.CancellationToken);
        Assert.Equal("Renamed", dto!.DisplayName);
        Assert.Equal(Mask, dto.Data["apiSecret"]); // still present (not blanked)
    }

    [Fact]
    public async Task Upsert_VisibleButNotImplemented_ReturnsValidationError()
    {
        var put = await PutAsync($"{Endpoint}/ghostpay",
            new { displayName = "Ghost Pay", visibility = true, data = new { } }, Admin());
        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }

    [Fact]
    public async Task SetVisibility_TogglesVoltspayments()
    {
        var token = Admin();

        var off = await PutAsync($"{Endpoint}/voltspayments/visibility", new { visible = false }, token);
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        var dto = await off.Content.ReadFromJsonAsync<ConfigDtoT>(TestContext.Current.CancellationToken);
        Assert.False(dto!.Visibility);

        var on = await PutAsync($"{Endpoint}/voltspayments/visibility", new { visible = true }, token);
        dto = await on.Content.ReadFromJsonAsync<ConfigDtoT>(TestContext.Current.CancellationToken);
        Assert.True(dto!.Visibility);
    }

    private sealed record ConfigDtoT(
        string KeyName, string DisplayName, bool Visibility, bool Implemented, Dictionary<string, string> Data);
}
