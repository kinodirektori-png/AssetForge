using AssetForge.Application;
using Microsoft.AspNetCore.Diagnostics;

namespace AssetForge.Api;

/// <summary>Maps errors to ProblemDetails with safe messages and no stack traces (claude.md).</summary>
public sealed class AppExceptionHandler(ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        IResult result = exception switch
        {
            ValidationFailedException v => Results.ValidationProblem(v.Errors),
            NotFoundException n => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: n.Message),
            ConflictException c => Results.Problem(statusCode: StatusCodes.Status409Conflict, title: c.Message),
            BadHttpRequestException bad => Results.Problem(statusCode: bad.StatusCode, title: "The request is invalid."),
            _ => Unexpected(exception),
        };
        await result.ExecuteAsync(context);
        return true;
    }

    private IResult Unexpected(Exception exception)
    {
        logger.LogError("Unhandled {ExceptionType}", exception.GetType().Name);
        return Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "An unexpected error occurred.");
    }
}
