using Xunit;

namespace DotnetTinyRun.Tests.Integration;

[Trait("Category", "Slow")]
public class ProjectContextLoaderTests
{
    private static string GetDemoProjectPath() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "demo", "MyApp", "MyApp.csproj"));

    [Fact]
    public async Task LoadAsync_WithDemoProject_LoadsContext()
    {
        var loader = new ProjectContextLoader();
        var context = await loader.LoadAsync(GetDemoProjectPath(), debug: false);

        Assert.NotNull(context);
        Assert.NotEmpty(context.MetadataReferences);
        Assert.NotEmpty(context.UsingDirectives);
        Assert.True(File.Exists(context.TargetAssemblyPath));
    }

    [Fact]
    public async Task LoadAsync_ProjectContext_ContainsMyAppNamespace()
    {
        var loader = new ProjectContextLoader();
        var context = await loader.LoadAsync(GetDemoProjectPath(), debug: false);

        Assert.Contains("MyApp", context.UsingDirectives);
    }

    [Fact]
    public async Task LoadAsync_ProjectContext_ContainsEfCoreReferences()
    {
        var loader = new ProjectContextLoader();
        var context = await loader.LoadAsync(GetDemoProjectPath(), debug: false);

        Assert.Contains(context.MetadataReferences,
            r => r.Display?.Contains("Microsoft.EntityFrameworkCore") == true);
    }

    [Fact]
    public async Task LoadAsync_NonExistentProject_ThrowsFileNotFoundException()
    {
        var loader = new ProjectContextLoader();

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => loader.LoadAsync("nonexistent.csproj", debug: false));
    }

    [Fact]
    public async Task RunAsync_WithProjectContext_CanAccessProjectTypes()
    {
        // Save and restore working directory since RunAsync changes it
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            var runner = new ScriptRunner();
            var options = new ScriptRunOptions
            {
                Code = """typeof(User).Name""",
                ProjectPath = GetDemoProjectPath(),
            };

            var exitCode = await runner.RunAsync(options);

            Assert.Equal(0, exitCode);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }
}
