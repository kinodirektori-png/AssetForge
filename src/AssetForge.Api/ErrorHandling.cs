using System.Text.Json;
using AssetForge.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace AssetForge.Api;

/// <summary>Maps errors to ProblemDetails with safe messages and no stack traces (claude.md).</summary>
public sealed class AppExceptionHandler(ILogger<AppExceptionHandler> logger, IOptions<JsonOptions> json) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        IResult result = exception switch
        {
            ValidationFailedException v => Results.ValidationProblem(ToJsonNames(v.Errors)),
            // The title is the same every time an error of this kind happens (RFC 9457); the specific text goes in detail.
            NotFoundException n => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: n.Message),
            ConflictException c => Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: c.Message),
            BadHttpRequestException bad => Results.Problem(statusCode: bad.StatusCode, title: "The request is invalid"),
            _ => Unexpected(exception),
        };
        await result.ExecuteAsync(context);
        return true;
    }

    /// <summary>
    /// The Application layer names fields by property (<c>DetailLevel</c>, <c>Palette[1]</c>). The keys are written the way the
    /// API writes its JSON properties, so they cannot drift from the request and response shapes.
    /// </summary>
    private Dictionary<string, string[]> ToJsonNames(IDictionary<string, string[]> errors)
    {
        var policy = json.Value.SerializerOptions.PropertyNamingPolicy;
        return errors.ToDictionary(e => policy is null ? e.Key : Rename(policy, e.Key), e => e.Value);
    }

    private static string Rename(JsonNamingPolicy policy, string key)
    {
        int bracket = key.IndexOf('[');
        return bracket < 0 ? policy.ConvertName(key) : policy.ConvertName(key[..bracket]) + key[bracket..];
    }

    private IResult Unexpected(Exception exception)
    {
        // The stack trace goes to the local log for diagnosis; the client only gets a generic message.
        // Exceptions must not carry keys, tokens or full prompts in their messages (claude.md).
        logger.LogError(exception, "Unhandled {ExceptionType}", exception.GetType().Name);
        return Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "An unexpected error occurred.");
    }
}
