using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AssetForge.Application.Projects;
using AssetForge.Infrastructure.Data;
using AssetForge.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssetForge.Tests.Api;

/// <summary>FR-1a and FR-2 through the API (spec §6, issue #5).</summary>
public sealed class ProjectEndpointsTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ProjectEndpointsTests() => _client = _factory.CreateAuthorizedClient();

    public void Dispose() => _factory.Dispose();

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private async Task<ProjectDto> CreateAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProjectDto>(Json))!;
    }

    private async Task<int> ProjectCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Projects.CountAsync();
    }

    private static async Task<Dictionary<string, string[]>> ErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("errors").Deserialize<Dictionary<string, string[]>>()!;
    }

    // ---- Projects -------------------------------------------------------------------------------------------

    [Fact]
    public async Task AC_1_1_new_project_appears_with_an_empty_style_guide()
    {
        var created = await CreateAsync("Dungeon");

        Assert.Equal("Dungeon", created.Name);
        Assert.Equal("dungeon", created.Slug);
        Assert.Equal(7, created.Id.Version);

        var list = await _client.GetFromJsonAsync<List<ProjectDto>>("/api/projects", Json);
        Assert.Equal(created, Assert.Single(list!));

        var guide = await _client.GetFromJsonAsync<StyleGuideDto>($"/api/projects/{created.Id}/style-guide", Json);
        Assert.NotNull(guide);
        Assert.All(new[] { guide.Description, guide.Perspective, guide.Lighting, guide.Outline, guide.DetailLevel }, Assert.Null);
        Assert.Empty(guide.Palette);
        Assert.Empty(guide.Avoid);
        Assert.Null(guide.UpdatedAt);
    }

    [Fact]
    public async Task Create_returns_location_and_the_project_can_be_read()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name = "  Forest  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ProjectDto>(Json))!;
        Assert.Equal($"/api/projects/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Forest", created.Name);
        Assert.Equal(created, await _client.GetFromJsonAsync<ProjectDto>($"/api/projects/{created.Id}", Json));
    }

    [Fact]
    public async Task List_is_oldest_first()
    {
        var a = await CreateAsync("Alpha");
        var b = await CreateAsync("Beta");
        var c = await CreateAsync("Gamma");

        var list = await _client.GetFromJsonAsync<List<ProjectDto>>("/api/projects", Json);

        // The fake clock steps one second per read, so the order comes from CreatedAt, not from timing or the ids.
        Assert.Equal([a.Id, b.Id, c.Id], list!.Select(p => p.Id));
        Assert.True(a.CreatedAt < b.CreatedAt && b.CreatedAt < c.CreatedAt);
        Assert.Equal(TimeSpan.Zero, a.CreatedAt.Offset);
    }

    [Fact]
    public async Task List_is_ordered_by_time_even_when_ids_are_not()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // The newer project gets the smaller id, so only CreatedAt can put them in the right order.
            var start = SteppingTimeProvider.Start;
            db.Projects.Add(new Project { Id = Guid.Parse("00000000-0000-7000-8000-000000000002"), Name = "Old", NormalizedName = "OLD", Slug = "old", CreatedAt = start });
            db.Projects.Add(new Project { Id = Guid.Parse("00000000-0000-7000-8000-000000000001"), Name = "New", NormalizedName = "NEW", Slug = "new", CreatedAt = start.AddMilliseconds(1) });
            await db.SaveChangesAsync();
        }

        var list = await _client.GetFromJsonAsync<List<ProjectDto>>("/api/projects", Json);

        Assert.Equal(["Old", "New"], list!.Select(p => p.Name));
    }

    [Theory]
    [InlineData("dungeon")]
    [InlineData("DUNGEON")]
    [InlineData(" Dungeon ")]
    public async Task AC_1_4_a_name_already_used_ignoring_case_is_refused(string name)
    {
        await CreateAsync("Dungeon");

        var response = await _client.PostAsJsonAsync("/api/projects", new { name });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("already exists", await response.Content.ReadAsStringAsync());
        Assert.Equal(1, await ProjectCountAsync());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"name\":null}")]
    [InlineData("{\"name\":\"\"}")]
    [InlineData("{\"name\":\"   \"}")]
    public async Task A_missing_or_blank_name_is_refused(string body)
    {
        var response = await _client.PostAsync("/api/projects", new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Contains("name", (await ErrorsAsync(response)).Keys);
        Assert.Equal(0, await ProjectCountAsync());
    }

    [Fact]
    public async Task A_name_of_50_characters_is_accepted_and_51_is_refused()
    {
        await CreateAsync(new string('a', 50));

        var errors = await ErrorsAsync(await _client.PostAsJsonAsync("/api/projects", new { name = new string('b', 51) }));

        Assert.Contains("50", Assert.Single(errors["name"]));
        Assert.Equal(1, await ProjectCountAsync());
    }

    [Fact]
    public async Task The_name_limit_counts_characters_not_code_units()
    {
        // Each 🐉 is two UTF-16 code units but one character (FR-1).
        string fifty = string.Concat(Enumerable.Repeat("🐉", 50));
        Assert.Equal(fifty, (await CreateAsync(fifty)).Name);

        var errors = await ErrorsAsync(await _client.PostAsJsonAsync("/api/projects", new { name = fifty + "x" }));
        Assert.Contains("50", Assert.Single(errors["name"]));
    }

    [Fact]
    public async Task Conflicts_share_one_title_and_name_the_project_in_detail()
    {
        await CreateAsync("Dungeon");
        await CreateAsync("Forest");

        var first = await ProblemAsync(await _client.PostAsJsonAsync("/api/projects", new { name = "dungeon" }));
        var second = await ProblemAsync(await _client.PostAsJsonAsync("/api/projects", new { name = "forest" }));

        Assert.Equal("Conflict", first.GetProperty("title").GetString());
        Assert.Equal("Conflict", second.GetProperty("title").GetString());
        Assert.Contains("dungeon", first.GetProperty("detail").GetString());
        Assert.Contains("forest", second.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Not_found_has_a_fixed_title()
    {
        var problem = await ProblemAsync(await _client.GetAsync($"/api/projects/{Guid.CreateVersion7()}"));

        Assert.Equal("Not found", problem.GetProperty("title").GetString());
        Assert.Equal("The project was not found.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task OpenApi_lists_the_collection_path_without_a_trailing_slash()
    {
        using var doc = JsonDocument.Parse(await _client.GetStringAsync("/openapi/v1.json"));
        var paths = doc.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/projects", out _));
        Assert.False(paths.TryGetProperty("/api/projects/", out _));
    }

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task Names_with_the_same_slug_get_different_slugs()
    {
        var first = await CreateAsync("Dungeon!");
        var second = await CreateAsync("Dungeon?");
        var symbols = await CreateAsync("!!!");

        Assert.Equal("dungeon", first.Slug);
        Assert.Equal("dungeon-2", second.Slug);
        Assert.Equal("project", symbols.Slug);
    }

    [Fact]
    public async Task Long_names_sharing_their_first_30_characters_get_distinct_slugs()
    {
        string stem = new('a', 30);

        var first = await CreateAsync(stem);
        var second = await CreateAsync(stem + "b");
        var third = await CreateAsync(stem + "c");

        Assert.Equal(3, new[] { first.Slug, second.Slug, third.Slug }.Distinct().Count());
        Assert.All(new[] { first, second, third }, p => Assert.True(p.Slug.Length <= Slugs.MaxLength));
    }

    [Fact]
    public async Task Names_that_look_identical_in_different_unicode_forms_are_duplicates()
    {
        await CreateAsync("Ch\u00e2teau");

        var response = await _client.PostAsJsonAsync("/api/projects", new { name = "Cha\u0302teau" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("x\ny")]
    [InlineData("a\u0000b")]
    [InlineData("Dun\u200Bgeon")]
    public async Task Names_with_control_or_invisible_characters_are_refused(string name)
    {
        var errors = await ErrorsAsync(await _client.PostAsJsonAsync("/api/projects", new { name }));

        Assert.Contains("control", Assert.Single(errors["name"]));
        Assert.Equal(0, await ProjectCountAsync());
    }

    [Fact]
    public async Task Zero_width_joiners_used_by_some_scripts_and_emoji_are_allowed()
    {
        var created = await CreateAsync("Family \U0001F468\u200D\U0001F469\u200D\U0001F467");

        Assert.Contains('\u200D', created.Name);
    }

    [Fact]
    public async Task Limits_also_cap_the_size_so_stacked_combining_marks_cannot_make_huge_values()
    {
        string huge = "a" + new string('\u0300', 200_000);   // one user-visible character, 200,000 UTF-16 units
        var project = await CreateAsync("Dungeon");
        string url = $"/api/projects/{project.Id}/style-guide";

        var name = await ErrorsAsync(await _client.PostAsJsonAsync("/api/projects", new { name = huge }));
        var guide = await ErrorsAsync(await _client.PutAsJsonAsync(url, new { description = huge, lighting = huge, avoid = new[] { huge } }));

        Assert.Contains("50", Assert.Single(name["name"]));
        Assert.Equal(["avoid[0]", "description", "lighting"], guide.Keys.Order());
        Assert.Equal(1, await ProjectCountAsync());
    }

    [Fact]
    public async Task Style_guide_limits_count_characters_a_user_sees_not_utf16_units()
    {
        var project = await CreateAsync("Dungeon");
        string url = $"/api/projects/{project.Id}/style-guide";

        var atLimit = await _client.PutAsJsonAsync(url, new { description = string.Concat(Enumerable.Repeat("\U0001F600", 300)) });
        Assert.Equal(HttpStatusCode.OK, atLimit.StatusCode);

        var over = await ErrorsAsync(await _client.PutAsJsonAsync(url,
            new { description = string.Concat(Enumerable.Repeat("\U0001F600", 301)) }));
        Assert.Contains("300", Assert.Single(over["description"]));
    }

    [Fact]
    public async Task A_long_invalid_colour_is_cut_on_whole_characters_in_the_error()
    {
        var project = await CreateAsync("Dungeon");
        string bad = "x" + string.Concat(Enumerable.Repeat("\U0001F600", 30));   // an odd first unit shifts the cut

        var errors = await ErrorsAsync(await _client.PutAsJsonAsync($"/api/projects/{project.Id}/style-guide", new { palette = new[] { bad } }));

        string message = Assert.Single(errors["palette[0]"]);
        Assert.Contains("x" + string.Concat(Enumerable.Repeat("\U0001F600", 19)) + "…", message);
        Assert.DoesNotContain('\uFFFD', message);
    }

    [Fact]
    public async Task Malformed_json_is_refused_with_400()
    {
        var response = await _client.PostAsync("/api/projects", new StringContent("{\"name\":", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ProjectCountAsync());
    }

    [Fact]
    public async Task Unknown_project_returns_404_problem_details()
    {
        string id = Guid.NewGuid().ToString();

        foreach (string path in new[] { $"/api/projects/{id}", $"/api/projects/{id}/style-guide" })
        {
            var response = await _client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        var put = await _client.PutAsJsonAsync($"/api/projects/{id}/style-guide", new { });
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    }

    [Theory]
    [InlineData("/api/projects/42")]
    [InlineData("/api/projects/not-a-guid/style-guide")]
    public async Task A_project_id_that_is_not_a_guid_returns_404(string path)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(path)).StatusCode);
    }

    // ---- Style guide ----------------------------------------------------------------------------------------

    private static object FullGuide() => new
    {
        description = "stylised hand-painted, soft shading",
        perspective = "3/4 top-down",
        palette = new[] { "#1a2b3c", "#FFFFFF" },
        lighting = "light from top-left",
        outline = "thin dark outline",
        detailLevel = "medium",
        avoid = new[] { "photorealism", "text", "watermark" },
    };

    [Fact]
    public async Task AC_2_1_a_saved_style_guide_reads_back_with_the_same_values()
    {
        var project = await CreateAsync("Dungeon");

        var put = await _client.PutAsJsonAsync($"/api/projects/{project.Id}/style-guide", FullGuide());
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var guide = (await _client.GetFromJsonAsync<StyleGuideDto>($"/api/projects/{project.Id}/style-guide", Json))!;
        Assert.Equal("stylised hand-painted, soft shading", guide.Description);
        Assert.Equal("3/4 top-down", guide.Perspective);
        Assert.Equal(["#1A2B3C", "#FFFFFF"], guide.Palette);
        Assert.Equal("light from top-left", guide.Lighting);
        Assert.Equal("thin dark outline", guide.Outline);
        Assert.Equal("medium", guide.DetailLevel);
        Assert.Equal(["photorealism", "text", "watermark"], guide.Avoid);
        Assert.NotNull(guide.UpdatedAt);
    }

    [Fact]
    public async Task AC_2_8_a_style_guide_without_a_palette_can_be_saved()
    {
        var project = await CreateAsync("Dungeon");

        var put = await _client.PutAsJsonAsync($"/api/projects/{project.Id}/style-guide",
            new { description = "pixel art", palette = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var guide = (await _client.GetFromJsonAsync<StyleGuideDto>($"/api/projects/{project.Id}/style-guide", Json))!;
        Assert.Equal("pixel art", guide.Description);
        Assert.Empty(guide.Palette);
        Assert.Null(guide.Perspective);
    }

    [Fact]
    public async Task Saving_replaces_the_whole_guide_and_an_empty_body_clears_it()
    {
        var project = await CreateAsync("Dungeon");
        await _client.PutAsJsonAsync($"/api/projects/{project.Id}/style-guide", FullGuide());

        var put = await _client.PutAsJsonAsync($"/api/projects/{project.Id}/style-guide", new { });

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var guide = (await _client.GetFromJsonAsync<StyleGuideDto>($"/api/projects/{project.Id}/style-guide", Json))!;
        Assert.Null(guide.Description);
        Assert.Empty(guide.Palette);
        Assert.Empty(guide.Avoid);
    }

    [Fact]
    public async Task Style_guides_of_different_projects_are_kept_apart()
    {
        var dungeon = await CreateAsync("Dungeon");
        var forest = await CreateAsync("Forest");

        await _client.PutAsJsonAsync($"/api/projects/{dungeon.Id}/style-guide", FullGuide());

        var forestGuide = (await _client.GetFromJsonAsync<StyleGuideDto>($"/api/projects/{forest.Id}/style-guide", Json))!;
        Assert.Null(forestGuide.Description);
    }

    [Fact]
    public async Task AC_2_2_seventeen_palette_colours_are_refused()
    {
        var project = await CreateAsync("Dungeon");
        var palette = Enumerable.Range(0, 17).Select(i => $"#0000{i:X2}").ToArray();

        var errors = await ErrorsAsync(await _client.PutAsJsonAsync($"/api/projects/{project.Id}/style-guide", new { palette }));

        Assert.Contains("16", Assert.Single(errors["palette"]));
    }

    [Fact]
    public async Task Sixteen_palette_colours_are_accepted()
    {
        var project = await CreateAsync("Dungeon");
        var palette = Enumerable.Range(0, 16).Select(i => $"#0000{i:X2}").ToArray();

        var put = await _client.PutAsJsonAsync($"/api/projects/{project.Id}/style-guide", new { palette });

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("123456")]
    [InlineData("#GGGGGG")]
    [InlineData("")]
    public async Task AC_2_3_an_invalid_hex_colour_is_refused_and_pointed_out(string bad)
    {
        var project = await CreateAsync("Dungeon");

        var errors = await ErrorsAsync(await _client.PutAsJsonAsync($"/api/projects/{project.Id}/style-guide",
            new { palette = new[] { "#000000", bad } }));

        Assert.Equal(["palette[1]"], errors.Keys);
    }

    [Theory]
    [InlineData("description", 300)]
    [InlineData("perspective", 100)]
    [InlineData("lighting", 100)]
    [InlineData("outline", 100)]
    [InlineData("detailLevel", 100)]
    public async Task AC_2_5_a_field_over_its_limit_is_refused_naming_field_and_limit(string field, int limit)
    {
        var project = await CreateAsync("Dungeon");
        string url = $"/api/projects/{project.Id}/style-guide";

        var atLimit = await _client.PutAsync(url, Body(field, new string('x', limit)));
        Assert.Equal(HttpStatusCode.OK, atLimit.StatusCode);

        var errors = await ErrorsAsync(await _client.PutAsync(url, Body(field, new string('x', limit + 1))));
        string message = Assert.Single(errors[field]);
        string words = System.Text.RegularExpressions.Regex.Replace(field, "(?<!^)([A-Z])", " $1");
        Assert.Contains(words, message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(limit.ToString(), message);

        static StringContent Body(string field, string value) =>
            new(JsonSerializer.Serialize(new Dictionary<string, string> { [field] = value }), Encoding.UTF8, "application/json");
    }

    [Fact]
    public async Task Avoid_terms_are_limited_in_count_and_length()
    {
        var project = await CreateAsync("Dungeon");
        string url = $"/api/projects/{project.Id}/style-guide";

        var tooMany = await ErrorsAsync(await _client.PutAsJsonAsync(url,
            new { avoid = Enumerable.Range(0, 21).Select(i => $"term{i}").ToArray() }));
        Assert.Contains("20", Assert.Single(tooMany["avoid"]));

        var tooLong = await ErrorsAsync(await _client.PutAsJsonAsync(url,
            new { avoid = new[] { "ok", new string('x', 41) } }));
        Assert.Contains("40", Assert.Single(tooLong["avoid[1]"]));

        var atLimits = await _client.PutAsJsonAsync(url,
            new { avoid = Enumerable.Range(0, 20).Select(_ => new string('x', 40)).ToArray() });
        Assert.Equal(HttpStatusCode.OK, atLimits.StatusCode);
    }

    [Fact]
    public async Task Values_are_trimmed_blanks_become_empty_and_colours_upper_case()
    {
        var project = await CreateAsync("Dungeon");
        string url = $"/api/projects/{project.Id}/style-guide";

        var put = await _client.PutAsync(url, new StringContent(
            """{"description":"   ","lighting":"  top-left  ","palette":[" #abcdef "],"avoid":[null,"  ","text"," blur "]}""",
            Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var guide = (await _client.GetFromJsonAsync<StyleGuideDto>(url, Json))!;
        Assert.Null(guide.Description);
        Assert.Equal("top-left", guide.Lighting);
        Assert.Equal(["#ABCDEF"], guide.Palette);
        Assert.Equal(["text", "blur"], guide.Avoid);
    }

    [Fact]
    public async Task A_null_palette_entry_is_refused_and_pointed_out()
    {
        var project = await CreateAsync("Dungeon");

        var errors = await ErrorsAsync(await _client.PutAsync($"/api/projects/{project.Id}/style-guide",
            new StringContent("""{"palette":[null]}""", Encoding.UTF8, "application/json")));

        Assert.Equal(["palette[0]"], errors.Keys);
    }

    [Fact]
    public async Task A_huge_palette_gets_one_error_not_one_per_entry()
    {
        var project = await CreateAsync("Dungeon");

        var errors = await ErrorsAsync(await _client.PutAsJsonAsync($"/api/projects/{project.Id}/style-guide",
            new { palette = Enumerable.Repeat("bad", 1000).ToArray() }));

        Assert.True(errors.Count <= StyleGuide.MaxPaletteColours + 1, $"{errors.Count} errors");
        Assert.Contains("palette", errors.Keys);
    }

    [Fact]
    public async Task An_invalid_guide_for_an_unknown_project_is_refused_before_lookup()
    {
        var response = await _client.PutAsJsonAsync($"/api/projects/{Guid.NewGuid()}/style-guide",
            new { palette = new[] { "nope" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_refused_save_leaves_the_stored_guide_unchanged()
    {
        var project = await CreateAsync("Dungeon");
        string url = $"/api/projects/{project.Id}/style-guide";
        await _client.PutAsJsonAsync(url, FullGuide());

        await _client.PutAsJsonAsync(url, new { description = "new", palette = new[] { "nope" } });

        var guide = (await _client.GetFromJsonAsync<StyleGuideDto>(url, Json))!;
        Assert.Equal("stylised hand-painted, soft shading", guide.Description);
    }

    // ---- Refused requests change nothing (spec §5) ----------------------------------------------------------

    [Fact]
    public async Task Refused_create_requests_store_no_project()
    {
        var noToken = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await noToken.PostAsJsonAsync("/api/projects", new { name = "A" })).StatusCode);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType,
            (await _client.PostAsync("/api/projects", new StringContent("name=A", Encoding.UTF8, "application/x-www-form-urlencoded"))).StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType,
            (await _client.PostAsync("/api/projects", null)).StatusCode);

        using var crossSite = new HttpRequestMessage(HttpMethod.Post, "/api/projects") { Content = JsonContent.Create(new { name = "A" }) };
        crossSite.Headers.Add("Origin", "https://evil.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(crossSite)).StatusCode);

        Assert.Equal(0, await ProjectCountAsync());
    }

    [Fact]
    public async Task Refused_style_guide_saves_change_nothing()
    {
        var project = await CreateAsync("Dungeon");
        string url = $"/api/projects/{project.Id}/style-guide";

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().PutAsJsonAsync(url, FullGuide())).StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType,
            (await _client.PutAsync(url, new StringContent("description=x", Encoding.UTF8, "text/plain"))).StatusCode);

        var guide = (await _client.GetFromJsonAsync<StyleGuideDto>(url, Json))!;
        Assert.Null(guide.Description);
        Assert.Null(guide.UpdatedAt);
    }
}
