using System.Diagnostics;
using Xunit;

namespace DotnetTinyRun.Tests.Integration;

[Collection("ConsoleOutput")]
[Trait("Category", "Integration")]
public class ScriptExecutionTests : IDisposable
{
    private readonly TextWriter _originalOut = Console.Out;
    private readonly TextWriter _originalError = Console.Error;
    private readonly StringWriter _stdout = new();
    private readonly StringWriter _stderr = new();
    private readonly ScriptRunner _runner = new();

    public ScriptExecutionTests()
    {
        Console.SetOut(_stdout);
        Console.SetError(_stderr);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
    }

    [Fact]
    public async Task RunAsync_SimpleExpression_Returns0()
    {
        var options = new ScriptRunOptions { Code = "1 + 1" };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsync_SimpleExpression_ProducesCorrectOutput()
    {
        var options = new ScriptRunOptions { Code = "1 + 1" };

        await _runner.RunAsync(options);

        Assert.Contains("2", _stdout.ToString());
    }

    [Fact]
    public async Task RunAsync_InvalidCode_Returns1()
    {
        var options = new ScriptRunOptions { Code = "this is not valid C#" };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task RunAsync_RuntimeException_Returns2()
    {
        var options = new ScriptRunOptions { Code = """int.Parse("not a number")""" };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task RunAsync_EmptyCode_Returns3()
    {
        var options = new ScriptRunOptions { Code = "   " };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(3, exitCode);
    }

    [Fact]
    public async Task RunAsync_NullCodeNoFileNoStdin_Returns3()
    {
        var options = new ScriptRunOptions();

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(3, exitCode);
    }

    [Fact]
    public async Task RunAsync_WithFilePath_ReadsAndExecutes()
    {
        var testFile = Path.Combine(AppContext.BaseDirectory, "TestData", "simple.csx");
        var options = new ScriptRunOptions { FilePath = testFile };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(0, exitCode);
        Assert.Contains("2", _stdout.ToString());
    }

    [Fact]
    public async Task RunAsync_DumpExtension_WorksAndSuppressesDuplicateOutput()
    {
        var options = new ScriptRunOptions { Code = """ "hello".Dump() """ };

        await _runner.RunAsync(options);

        var output = _stdout.ToString();
        // "hello" should appear exactly once — Dump() prints internally, WriteResult is skipped
        var count = output.Split("hello").Length - 1;
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RunAsync_MultipleStatementsWithSemicolons_Works()
    {
        var options = new ScriptRunOptions { Code = "var x = 10; var y = 20; x + y" };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(0, exitCode);
        Assert.Contains("30", _stdout.ToString());
    }

    [Fact]
    public async Task RunAsync_DebugFlag_ProducesDebugOutput()
    {
        var options = new ScriptRunOptions { Code = "1", Debug = true };

        await _runner.RunAsync(options);

        Assert.Contains("[DEBUG]", _stderr.ToString());
    }

    [Fact]
    public async Task RunAsync_LinqExpression_Works()
    {
        var options = new ScriptRunOptions { Code = "Enumerable.Range(1, 5).Sum()" };

        var exitCode = await _runner.RunAsync(options);

        Assert.Equal(0, exitCode);
        Assert.Contains("15", _stdout.ToString());
    }

    [Fact]
    public void HelpViaSubprocess_OutputContainsUsage()
    {
        // Find the src/DotnetTinyRun directory by walking up from test bin directory
        var testDir = AppContext.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
        var srcDir = Path.Combine(repoRoot, "src", "DotnetTinyRun");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run -- --help",
            WorkingDirectory = srcDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        Assert.Contains("DotnetTinyRun", output);
        Assert.Contains("--project", output);
        Assert.Contains("--file", output);
    }
}
