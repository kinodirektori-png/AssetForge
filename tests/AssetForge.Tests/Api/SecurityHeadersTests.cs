using AssetForge.Api.Security;
using AssetForge.Tests.Support;

namespace AssetForge.Tests.Api;

/// <summary>Review of #4: Scalar may only load its own bundled assets, and API responses get a locked-down policy.</summary>
public sealed class SecurityHeadersTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Scalar_page_gets_the_self_only_policy()
    {
        var response = await _factory.CreateClient().GetAsync("/scalar/v1");

        Assert.Equal(SecurityHeadersMiddleware.ScalarPolicy, response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Theory]
    [InlineData("/api/status")]
    [InlineData("/openapi/v1.json")]
    public async Task Other_responses_get_the_api_policy(string path)
    {
        var response = await _factory.CreateAuthorizedClient().GetAsync(path);

        Assert.Equal(SecurityHeadersMiddleware.ApiPolicy, response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task Refused_requests_also_get_the_headers()
    {
        var response = await _factory.CreateClient().GetAsync("/api/status");

        Assert.Equal(SecurityHeadersMiddleware.ApiPolicy, response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public void Scalar_policy_allows_no_other_origin()
    {
        Assert.DoesNotContain("http", SecurityHeadersMiddleware.ScalarPolicy);
        Assert.DoesNotContain("*", SecurityHeadersMiddleware.ScalarPolicy);
    }
}
