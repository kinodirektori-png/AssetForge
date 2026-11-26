using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AssetForge.Tests.Support;

/// <summary>The real API with a temp SQLite file. Fakes for external calls are added by the feature issues.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Token = "test-token-0123456789-abcdefghijklmnop";
    public const string RemoteIpHeader = "X-Test-Remote-Ip";

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "assetforge-tests", Guid.NewGuid().ToString("N"));

    public string? TokenSetting { get; init; } = Token;
    public Dictionary<string, string?> Settings { get; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_dir);
        builder.UseEnvironment("Testing");
        builder.UseSetting("Security:ApiToken", TokenSetting ?? "");
        builder.UseSetting("ConnectionStrings:AssetForge", $"Data Source={Path.Combine(_dir, "test.db")}");
        foreach (var (key, value) in Settings)
            builder.UseSetting(key, value);

        builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter, TestRemoteIpStartupFilter>());
    }

    /// <summary>A client that sends the API token, like the owner's Scalar session.</summary>
    public HttpClient CreateAuthorizedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>The test server has no real connection, so give it a loopback address unless a test asks otherwise.</summary>
    private sealed class TestRemoteIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                string? ip = context.Request.Headers[RemoteIpHeader];
                context.Connection.RemoteIpAddress = ip is null ? IPAddress.Loopback : IPAddress.Parse(ip);
                await nextMiddleware();
            });
            next(app);
        };
    }
}
