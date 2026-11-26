using System.Net;
using Microsoft.Net.Http.Headers;

namespace AssetForge.Api.Security;

/// <summary>
/// FR-14 and spec §5, layers 1, 4 and 5: loopback clients only, no cross-site or cross-origin browser requests,
/// and a JSON body on every POST and PUT. Runs after host filtering (layer 2) and before the API token check (layer 3).
/// </summary>
public sealed class LocalOnlyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        if (remote is null || !IPAddress.IsLoopback(remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote))
        {
            await Problem(context, StatusCodes.Status403Forbidden, "Only requests from this computer are accepted.");
            return;
        }

        string fetchSite = context.Request.Headers["Sec-Fetch-Site"].ToString();
        if (fetchSite is "cross-site" or "same-site")
        {
            await Problem(context, StatusCodes.Status403Forbidden, "Cross-site requests are refused.");
            return;
        }

        string origin = context.Request.Headers.Origin.ToString();
        if (origin.Length > 0 && !string.Equals(origin, $"{context.Request.Scheme}://{context.Request.Host}", StringComparison.OrdinalIgnoreCase))
        {
            await Problem(context, StatusCodes.Status403Forbidden, "Cross-origin requests are refused.");
            return;
        }

        // Every POST, PUT and PATCH needs a JSON body ({} where nothing else is needed), and any body must be JSON.
        var request = context.Request;
        bool needsBody = HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method) || HttpMethods.IsPatch(request.Method);
        bool hasContent = request.ContentLength > 0 || request.Headers.TransferEncoding.Count > 0;
        bool declaresBody = hasContent || !string.IsNullOrEmpty(request.ContentType);
        if ((needsBody && !hasContent) || (declaresBody && !IsJson(request.ContentType)))
        {
            await Problem(context, StatusCodes.Status415UnsupportedMediaType,
                "POST and PUT requests need a JSON body with Content-Type: application/json.");
            return;
        }

        await next(context);
    }

    private static bool IsJson(string? contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out var media) &&
        string.Equals(media.MediaType.Value, "application/json", StringComparison.OrdinalIgnoreCase);

    internal static Task Problem(HttpContext context, int status, string title) =>
        Results.Problem(statusCode: status, title: title).ExecuteAsync(context);
}
