using System.Net;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

[Collection("SharedTestContainers")]
public class HealthTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(HealthTests);

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
