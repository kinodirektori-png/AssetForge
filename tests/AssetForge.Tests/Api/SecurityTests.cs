using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AssetForge.Tests.Support;
using AssetForge.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace AssetForge.Tests.Api;

/// <summary>
/// FR-14 and spec §5. Each layer is checked on the token-protected status endpoint here; the feature issues add
/// "no model call, no spend, no file" checks on their own endpoints.
/// </summary>
public sealed class SecurityTests : IDisposable
{
    private const string Status = "/api/status";
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static StringContent JsonBody(string json = "{}") => new(json, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Valid_token_from_localhost_is_accepted()
    {
        var response = await _factory.CreateAuthorizedClient().GetAsync(Status);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"ok\"", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("localhost;127.0.0.1")]
    [InlineData(" LOCALHOST ")]
    public void App_starts_with_local_allowed_hosts(string allowedHosts)
    {
        using var factory = new ApiFactory();
        factory.Settings["AllowedHosts"] = allowedHosts;

        using var client = factory.CreateClient();
    }

    [Fact]
    public async Task Missing_token_is_refused_with_401_problem_details()
    {
        var response = await _factory.CreateClient().GetAsync(Status);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.Single().Scheme);
    }

    [Theory]
    [InlineData("Bearer wrong-token")]
    [InlineData("Bearer " + ApiFactory.Token + "x")]
    [InlineData("Basic dGVzdA==")]
    [InlineData("Bearer ")]
    public async Task Wrong_token_is_refused_with_401(string authorization)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Status)).StatusCode);
    }

    [Fact]
    public async Task Allowlisted_public_paths_work_without_token()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var root = await client.GetAsync("/");
        var openApi = await client.GetAsync("/openapi/v1.json");
        var scalar = await client.GetAsync("/scalar/v1");

        Assert.Equal("/scalar/v1", root.Headers.Location?.ToString());
        Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
        Assert.Equal(HttpStatusCode.OK, scalar.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    [InlineData("/scalar/v1")]
    [InlineData("/scalar/scalar.js")]
    public void Public_allowlist_is_exactly_root_openapi_and_scalar(string path)
    {
        Assert.True(ApiTokenMiddleware.IsPublic(path));
    }

    [Theory]
    [InlineData("/api/status")]
    [InlineData("/images/123/file")]
    [InlineData("/health")]
    [InlineData("/openapix")]
    [InlineData("/scalarx/v1")]
    [InlineData("/x/openapi")]
    public void Everything_else_needs_the_token(string path)
    {
        Assert.False(ApiTokenMiddleware.IsPublic(path));
    }

    [Fact]
    public async Task Every_mapped_endpoint_outside_the_allowlist_returns_401_without_token()
    {
        // Deny by default: a new endpoint anywhere is protected unless it is deliberately made public.
        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();
        var client = _factory.CreateClient();
        var checkedRoutes = new List<string>();

        foreach (var endpoint in endpoints)
        {
            string path = "/" + string.Join("/", endpoint.RoutePattern.PathSegments.Select(segment =>
                string.Concat(segment.Parts.Select(part => part switch
                {
                    RoutePatternLiteralPart literal => literal.Content,
                    RoutePatternParameterPart => Guid.NewGuid().ToString(),
                    _ => "x",
                }))));
            if (ApiTokenMiddleware.IsPublic(path))
                continue;

            var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"];
            foreach (var method in methods)
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), path);
                if (method is "POST" or "PUT" or "PATCH")
                    request.Content = JsonBody();

                var response = await client.SendAsync(request);

                Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"{method} {path} returned {(int)response.StatusCode} without a token.");
                checkedRoutes.Add($"{method} {path}");
            }
        }

        Assert.Contains("GET /api/status", checkedRoutes);
    }

    [Theory]
    [InlineData("/not-mapped")]
    [InlineData("/data/assetforge.db")]
    public async Task Unmapped_paths_also_need_the_token(string path)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_declares_bearer_auth()
    {
        string json = await _factory.CreateClient().GetStringAsync("/openapi/v1.json");

        Assert.Contains("\"bearer\"", json);
        Assert.Contains("/api/status", json);
    }

    [Fact]
    public async Task Preflight_gets_no_cors_headers()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, Status);
        request.Headers.Add("Origin", "http://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.False(response.IsSuccessStatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("evil.example")]
    [InlineData("evil.example:5062")]
    [InlineData("192.168.1.10")]
    [InlineData("[::1]:5062")]
    public async Task Host_other_than_localhost_or_127_0_0_1_is_refused_with_400(string host)
    {
        // AC-14.3: a DNS-rebinding page sends its own host name.
        var client = _factory.CreateAuthorizedClient();
        client.DefaultRequestHeaders.Host = host;

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Status)).StatusCode);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("localhost:5062")]
    [InlineData("127.0.0.1:5062")]
    public async Task Localhost_and_127_0_0_1_are_accepted(string host)
    {
        var client = _factory.CreateAuthorizedClient();
        client.DefaultRequestHeaders.Host = host;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Status)).StatusCode);
    }

    [Theory]
    [InlineData("Origin", "http://evil.example")]
    [InlineData("Origin", "http://localhost:3000")]
    [InlineData("Origin", "null")]
    [InlineData("Sec-Fetch-Site", "cross-site")]
    [InlineData("Sec-Fetch-Site", "same-site")]
    public async Task Request_from_another_origin_is_refused_with_403(string header, string value)
    {
        // AC-14.3: a page from another site, or another local app, in the owner's browser.
        var client = _factory.CreateAuthorizedClient();
        client.DefaultRequestHeaders.Add(header, value);

        var get = await client.GetAsync(Status);
        var post = await client.PostAsync(Status, JsonBody());

        Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
    }

    [Theory]
    [InlineData("same-origin")]
    [InlineData("none")]
    public async Task Same_origin_browser_request_is_accepted(string fetchSite)
    {
        var client = _factory.CreateAuthorizedClient();
        client.DefaultRequestHeaders.Add("Origin", "http://localhost");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", fetchSite);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Status)).StatusCode);
    }

    [Fact]
    public async Task Post_without_a_body_is_refused_with_415()
    {
        var response = await _factory.CreateAuthorizedClient().PostAsync(Status, null);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Put_with_json_content_type_but_empty_body_is_refused_with_415()
    {
        var response = await _factory.CreateAuthorizedClient().PutAsync(Status, JsonBody(""));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Theory]
    [InlineData("application/x-www-form-urlencoded", "a=b")]
    [InlineData("text/plain", "{}")]
    [InlineData("multipart/form-data; boundary=x", "--x--")]
    public async Task Non_json_body_is_refused_with_415(string contentType, string body)
    {
        var content = new StringContent(body, Encoding.UTF8);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        var response = await _factory.CreateAuthorizedClient().PostAsync(Status, content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Json_body_passes_the_access_layers()
    {
        // The status endpoint only answers GET, so 405 shows the request got past every layer.
        var response = await _factory.CreateAuthorizedClient().PostAsync(Status, JsonBody());

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Theory]
    [InlineData("10.0.0.5")]
    [InlineData("192.168.1.20")]
    [InlineData("::ffff:10.0.0.5")]
    public async Task Request_from_another_device_is_refused_with_403(string ip)
    {
        // AC-14.1 and AC-14.2.
        var client = _factory.CreateAuthorizedClient();
        client.DefaultRequestHeaders.Add(ApiFactory.RemoteIpHeader, ip);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Status)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/openapi/v1.json")).StatusCode);
    }

    [Theory]
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]
    public async Task Loopback_addresses_are_accepted(string ip)
    {
        var client = _factory.CreateAuthorizedClient();
        client.DefaultRequestHeaders.Add(ApiFactory.RemoteIpHeader, ip);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Status)).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too-short")]
    public void App_does_not_start_without_a_strong_token(string? token)
    {
        using var factory = new ApiFactory { TokenSetting = token };

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Security:ApiToken", ex.ToString());
    }

    [Theory]
    [InlineData("*")]
    [InlineData("localhost;*")]
    [InlineData("localhost;evil.example")]
    [InlineData("localhost;[::1]")]
    [InlineData("")]
    [InlineData(";")]
    public void App_does_not_start_with_any_host_but_localhost_and_127_0_0_1(string allowedHosts)
    {
        using var factory = new ApiFactory();
        factory.Settings["AllowedHosts"] = allowedHosts;

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("AllowedHosts", ex.ToString());
    }
}
