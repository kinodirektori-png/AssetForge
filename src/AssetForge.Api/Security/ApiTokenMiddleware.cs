using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace AssetForge.Api.Security;

/// <summary>
/// Spec §5 layer 3: every request needs <c>Authorization: Bearer &lt;token&gt;</c>, except a short allowlist of public paths. A web page cannot know the token,
/// and the custom header forces a CORS preflight that this API never answers. Compared in constant time, never logged.
/// </summary>
public sealed class ApiTokenMiddleware(RequestDelegate next, IOptions<SecurityOptions> options)
{
    private readonly byte[] _expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.ApiToken ?? ""));

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsPublic(context.Request.Path) && !HasValidToken(context.Request))
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await LocalOnlyMiddleware.Problem(context, StatusCodes.Status401Unauthorized, "A valid API token is required.");
            return;
        }
        await next(context);
    }

    /// <summary>
    /// Deny by default: only these paths work without the token. They hold no data: the redirect to Scalar,
    /// the OpenAPI document and the Scalar UI with its bundled assets.
    /// </summary>
    internal static bool IsPublic(PathString path) =>
        path == "/" || path.StartsWithSegments("/openapi") || path.StartsWithSegments("/scalar");

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
