using System.Net;
using AssetForge.Api.Security;
using AssetForge.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task Server_errors_keep_the_headers()
    {
        using var factory = _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.AddTransient<IStartupFilter, ThrowAtEndOfPipeline>()));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", ApiFactory.Token);

        var response = await client.GetAsync("/api/boom");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(SecurityHeadersMiddleware.ApiPolicy, response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    /// <summary>Adds a last middleware that throws, so an authorized request to an unmapped path ends in a 500.</summary>
    private sealed class ThrowAtEndOfPipeline : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Run(_ => throw new InvalidOperationException("boom"));
        };
    }
}
