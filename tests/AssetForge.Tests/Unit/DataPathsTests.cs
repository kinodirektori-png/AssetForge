using AssetForge.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace AssetForge.Tests.Unit;

/// <summary>NFR-6, NFR-7: data lives in the user profile, not in the repository or next to the app.</summary>
public sealed class DataPathsTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value))).Build();

    [Fact]
    public void Default_is_AssetForge_under_local_app_data()
    {
        string expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AssetForge");

        Assert.Equal(expected, DataPaths.Root(Config()));
        Assert.DoesNotContain(Path.Combine("src", "AssetForge.Api"), DataPaths.Root(Config()));
    }

    [Fact]
    public void Data_root_can_be_configured()
    {
        string custom = Path.Combine(Path.GetTempPath(), "af-data");

        Assert.Equal(Path.GetFullPath(custom), DataPaths.Root(Config(("Storage:DataRoot", custom))));
    }

    [Fact]
    public void Blank_setting_falls_back_to_the_default()
    {
        Assert.Equal(DataPaths.Root(Config()), DataPaths.Root(Config(("Storage:DataRoot", "  "))));
    }

    [Fact]
    public async Task Without_a_connection_string_the_database_is_created_under_the_data_root()
    {
        string root = Path.Combine(Path.GetTempPath(), "af-data-" + Guid.NewGuid().ToString("N"));
        using var factory = new Support.ApiFactory();
        factory.Settings["ConnectionStrings:AssetForge"] = "";
        factory.Settings["Storage:DataRoot"] = root;

        try
        {
            var client = factory.CreateAuthorizedClient();
            Assert.True((await client.GetAsync("/api/status")).IsSuccessStatusCode);
            Assert.True(File.Exists(Path.Combine(root, "assetforge.db")));
        }
        finally
        {
            factory.Dispose();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }
}
