using System.Text.Json;
using System.Text.Json.Serialization;
using AssetForge.Api;
using AssetForge.Api.Endpoints;
using AssetForge.Api.Security;
using AssetForge.Application;
using AssetForge.Infrastructure;
using AssetForge.Infrastructure.Data;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Spec §5 layer 1: listen on loopback only.
int port = builder.Configuration.GetValue("Server:Port", 5062);
builder.WebHost.ConfigureKestrel(k => k.ListenLocalhost(port));

builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.Section));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((doc, _, _) =>
{
    doc.Info.Title = "Asset Forge API";
    doc.Components ??= new OpenApiComponents();
    doc.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    doc.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "The Security:ApiToken value from user-secrets.",
    };
    doc.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", doc)] = [] }];
    return Task.CompletedTask;
}));

var app = builder.Build();

StartupChecks.Run(app.Configuration);

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureMigratedAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
// Spec §5 layer 2 (host filtering on AllowedHosts) is added by the host before this point.
app.UseMiddleware<LocalOnlyMiddleware>();
app.UseMiddleware<ApiTokenMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
app.MapStatus();

app.Run();

public partial class Program;
