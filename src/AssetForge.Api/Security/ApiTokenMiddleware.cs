using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace AssetForge.Api.Security;

/// <summary>
/// Spec §5 layer 3: every /api request needs <c>Authorization: Bearer &lt;token&gt;</c>. A web page cannot know the token,
/// and the custom header forces a CORS preflight that this API never answers. Compared in constant time, never logged.
/// </summary>
public sealed class ApiTokenMiddleware(RequestDelegate next, IOptions<SecurityOptions> options)
{
    private readonly byte[] _expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.ApiToken ?? ""));

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api") && !HasValidToken(context.Request))
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await LocalOnlyMiddleware.Problem(context, StatusCodes.Status401Unauthorized, "A valid API token is required.");
            return;
        }
        await next(context);
    }

    private bool HasValidToken(HttpRequest request)
    {
        string header = request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        byte[] actual = SHA256.HashData(Encoding.UTF8.GetBytes(header[prefix.Length..].Trim()));
        return CryptographicOperations.FixedTimeEquals(actual, _expectedHash);
    }
}
