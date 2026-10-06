using System.Net;

namespace MucBackend.Tests;

public class SmokeTests(MucApp app) : IClassFixture<MucApp>
{
    [Fact]
    public async Task Health_endpoint_responds()
    {
        var response = await app.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
