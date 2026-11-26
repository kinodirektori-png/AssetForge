namespace AssetForge.Api.Endpoints;

public sealed record StatusDto(string Status);

public static class StatusEndpoints
{
    /// <summary>A token-protected liveness check, so Scalar and the tests can confirm the access layers.</summary>
    public static void MapStatus(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/status", () => new StatusDto("ok")).WithTags("Status");
}
