using AssetForge.Application.Projects;

namespace AssetForge.Api.Endpoints;

public sealed record CreateProjectBody(string? Name);

/// <summary>FR-1a projects and the FR-2 style guide (spec §6). Validation happens in <see cref="ProjectService"/>.</summary>
public static class ProjectEndpoints
{
    public static void MapProjects(this IEndpointRouteBuilder app)
    {
        var projects = app.MapGroup("/api/projects").WithTags("Projects");

        projects.MapPost("/", async (CreateProjectBody body, ProjectService service, CancellationToken ct) =>
            {
                var project = await service.CreateAsync(body.Name, ct);
                return TypedResults.Created($"/api/projects/{project.Id}", project);
            })
            .WithSummary("Create a project with an empty style guide")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        projects.MapGet("/", (ProjectService service, CancellationToken ct) => service.ListAsync(ct))
            .WithSummary("List projects, oldest first");

        projects.MapGet("/{id:guid}", (Guid id, ProjectService service, CancellationToken ct) => service.GetAsync(id, ct))
            .WithSummary("Read a project")
            .ProducesProblem(StatusCodes.Status404NotFound);

        projects.MapGet("/{id:guid}/style-guide",
                (Guid id, ProjectService service, CancellationToken ct) => service.GetStyleGuideAsync(id, ct))
            .WithSummary("Read a project's style guide")
            .ProducesProblem(StatusCodes.Status404NotFound);

        projects.MapPut("/{id:guid}/style-guide",
                (Guid id, StyleGuideInput body, ProjectService service, CancellationToken ct) =>
                    service.SaveStyleGuideAsync(id, body, ct))
            .WithSummary("Save a project's style guide; every field may be empty")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
