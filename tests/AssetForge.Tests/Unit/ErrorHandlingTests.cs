using System.Text.Json;
using AssetForge.Api;
using AssetForge.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssetForge.Tests.Unit;

/// <summary>Errors become ProblemDetails with safe messages and no stack traces (claude.md).</summary>
public sealed class ErrorHandlingTests
{
    private static async Task<(int Status, JsonElement Body)> Handle(Exception exception)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();

        bool handled = await new AppExceptionHandler(NullLogger<AppExceptionHandler>.Instance)
            .TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, doc.RootElement.Clone());
    }

    [Fact]
    public async Task Validation_errors_are_400_with_field_errors()
    {
        var (status, body) = await Handle(new ValidationFailedException("name", "Name is required."));

        Assert.Equal(400, status);
        Assert.Equal("Name is required.", body.GetProperty("errors").GetProperty("name")[0].GetString());
    }

    [Fact]
    public async Task Not_found_is_404()
    {
        var (status, body) = await Handle(new NotFoundException("Project"));

        Assert.Equal(404, status);
        Assert.Equal("Project was not found.", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Conflict_is_409()
    {
        var (status, body) = await Handle(new ConflictException("Not allowed now."));

        Assert.Equal(409, status);
        Assert.Equal("Not allowed now.", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Bad_request_from_model_binding_is_400()
    {
        var (status, _) = await Handle(new BadHttpRequestException("bad json", 400));

        Assert.Equal(400, status);
    }

    [Fact]
    public async Task Unexpected_error_is_500_without_its_message_or_stack()
    {
        var (status, body) = await Handle(new InvalidOperationException("secret detail at C:\\path"));

        Assert.Equal(500, status);
        string text = body.GetRawText();
        Assert.DoesNotContain("secret detail", text);
        Assert.DoesNotContain("InvalidOperationException", text);
        Assert.Equal("An unexpected error occurred.", body.GetProperty("title").GetString());
    }
}
