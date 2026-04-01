using Xunit;

namespace DotnetTinyRun.Tests.Integration;

/// <summary>
/// Integration tests validating that DotnetTinyRun can load context from the AspNetApp
/// and UnityGameLib demo projects and execute scripts against them.
/// </summary>
[Trait("Category", "Slow")]
public class DemoProjectTests : IDisposable
{
    private readonly TextWriter _originalOut = Console.Out;
    private readonly TextWriter _originalError = Console.Error;
    private readonly StringWriter _stdout = new();
    private readonly StringWriter _stderr = new();
    private readonly ScriptRunner _runner = new();
    private readonly string _originalDir = Directory.GetCurrentDirectory();

    public DemoProjectTests()
    {
        Console.SetOut(_stdout);
        Console.SetError(_stderr);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        Directory.SetCurrentDirectory(_originalDir);
    }

    private static string GetRepoRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string GetAspNetAppProjectPath() =>
        Path.Combine(GetRepoRoot(), "demo", "AspNetApp", "AspNetApp.csproj");

    private static string GetUnityGameLibProjectPath() =>
        Path.Combine(GetRepoRoot(), "demo", "UnityGameLib", "UnityGameLib.csproj");

    // ── ASP.NET App Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task AspNetApp_LoadContext_ContainsAspNetAppAssemblies()
    {
        var loader = new ProjectContextLoader();
        var context = await loader.LoadAsync(GetAspNetAppProjectPath(), debug: false);

        Assert.NotNull(context);
        Assert.NotEmpty(context.MetadataReferences);
        Assert.True(File.Exists(context.TargetAssemblyPath));
    }

    [Fact]
    public async Task AspNetApp_LoadContext_ContainsProjectNamespace()
    {
        var loader = new ProjectContextLoader();
        var context = await loader.LoadAsync(GetAspNetAppProjectPath(), debug: false);

        Assert.Contains(context.UsingDirectives, u => u.Contains("AspNetApp"));
    }

    [Fact]
    public async Task AspNetApp_LoadContext_ContainsEfCoreReferences()
    {
        var loader = new ProjectContextLoader();
        var context = await loader.LoadAsync(GetAspNetAppProjectPath(), debug: false);

        Assert.Contains(context.MetadataReferences,
            r => r.Display?.Contains("Microsoft.EntityFrameworkCore") == true);
    }

    [Fact]
    public async Task AspNetApp_LoadContext_ContainsAspNetCoreFrameworkReferences()
    {
        var loader = new ProjectContextLoader();
        var context = await loader.LoadAsync(GetAspNetAppProjectPath(), debug: false);

        // Must include shared framework DLLs like Microsoft.Extensions.DependencyInjection.Abstractions
        Assert.Contains(context.MetadataReferences,
            r => r.Display?.Contains("Microsoft.Extensions.DependencyInjection.Abstractions") == true);
    }

    [Fact]
    public async Task AspNetApp_InlineScript_CanAccessProductType()
    {
        var options = new ScriptRunOptions
        {
            Code = "typeof(AspNetApp.Models.Product).Name",
            ProjectPath = GetAspNetAppProjectPath(),
        };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(0, exitCode);
        Assert.Contains("Product", _stdout.ToString());
    }

    [Fact]
    public async Task AspNetApp_InlineScript_CanReflectOnOrderModel()
    {
        var options = new ScriptRunOptions
        {
            Code = "typeof(AspNetApp.Models.Order).GetProperties().Length",
            ProjectPath = GetAspNetAppProjectPath(),
        };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(0, exitCode);
        // Order has Id, CustomerName, OrderDate, Items, Status, TotalAmount = 6 properties
        Assert.Contains("6", _stdout.ToString());
    }

    // ── Unity Game Library Tests ────────────────────────────────────────────

    [Fact]
    public async Task UnityGameLib_LoadContext_ContainsGameLibAssembly()
    {
        var loader = new ProjectContextLoader();
        var context = await loader.LoadAsync(GetUnityGameLibProjectPath(), debug: false);

        Assert.NotNull(context);
        Assert.NotEmpty(context.MetadataReferences);
        Assert.Contains(context.MetadataReferences,
            r => r.Display?.Contains("UnityGameLib") == true);
    }

    [Fact]
    public async Task UnityGameLib_LoadContext_ContainsGameNamespaces()
    {
        var loader = new ProjectContextLoader();
        var context = await loader.LoadAsync(GetUnityGameLibProjectPath(), debug: false);

        Assert.Contains(context.UsingDirectives, u => u.Contains("UnityGameLib"));
    }

    [Fact]
    public async Task UnityGameLib_InlineScript_Vector3Operations()
    {
        var options = new ScriptRunOptions
        {
            Code = "new UnityGameLib.Math.Vector3(3, 4, 0).Magnitude",
            ProjectPath = GetUnityGameLibProjectPath(),
        };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(0, exitCode);
        Assert.Contains("5", _stdout.ToString());
    }

    [Fact]
    public async Task UnityGameLib_InlineScript_CharacterCreation()
    {
        var options = new ScriptRunOptions
        {
            Code = "UnityGameLib.Systems.Character.Create(\"Hero\", UnityGameLib.Systems.CharacterClass.Warrior, level: 1).Name",
            ProjectPath = GetUnityGameLibProjectPath(),
        };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(0, exitCode);
        Assert.Contains("Hero", _stdout.ToString());
    }

    [Fact]
    public async Task UnityGameLib_ScriptFile_GameMath_RunsSuccessfully()
    {
        var scriptPath = Path.Combine(GetRepoRoot(), "demo", "UnityGameLib", "scripts", "game-math.csx");

        var options = new ScriptRunOptions
        {
            FilePath = scriptPath,
            ProjectPath = GetUnityGameLibProjectPath(),
        };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(0, exitCode);
        var output = _stdout.ToString();
        Assert.Contains("Vector3 Math Operations", output);
        Assert.Contains("Distance: 5.00", output);
    }

    [Fact]
    public async Task UnityGameLib_ScriptFile_CombatBalance_RunsSuccessfully()
    {
        var scriptPath = Path.Combine(GetRepoRoot(), "demo", "UnityGameLib", "scripts", "combat-balance.csx");

        var options = new ScriptRunOptions
        {
            FilePath = scriptPath,
            ProjectPath = GetUnityGameLibProjectPath(),
        };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(0, exitCode);
        var output = _stdout.ToString();
        Assert.Contains("Combat Balance Simulation", output);
        Assert.Contains("Win Count", output);
    }
}
