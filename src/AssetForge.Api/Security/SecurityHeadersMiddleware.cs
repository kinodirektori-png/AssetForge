namespace AssetForge.Api.Security;

/// <summary>
/// Spec §5, defence in depth for the token kept in Scalar's browser storage: a Content-Security-Policy that lets pages
/// load scripts, styles and data only from this app and send requests only back to it. A third-party script cannot run
/// on this origin, and no script can send the token anywhere else. Scalar ships its assets inside the package.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    internal const string ScalarPolicy =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "font-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    internal const string ApiPolicy = "default-src 'none'; frame-ancestors 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.ContentSecurityPolicy = context.Request.Path.StartsWithSegments("/scalar") ? ScalarPolicy : ApiPolicy;
        headers.XContentTypeOptions = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";
        return next(context);
    }
}
