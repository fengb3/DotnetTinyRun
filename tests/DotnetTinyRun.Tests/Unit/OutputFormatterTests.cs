using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DotnetTinyRun.Tests.Unit;

[Collection("ConsoleOutput")]
public class OutputFormatterTests : IDisposable
{
    private readonly TextWriter _originalOut = Console.Out;
    private readonly TextWriter _originalError = Console.Error;
    private readonly StringWriter _stdout = new();
    private readonly StringWriter _stderr = new();

    public OutputFormatterTests()
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
    public void WriteResult_Null_WritesNothing()
    {
        OutputFormatter.WriteResult(null);

        Assert.Equal(string.Empty, _stdout.ToString());
    }

    [Fact]
    public void WriteResult_Integer_WritesValue()
    {
        OutputFormatter.WriteResult(42);

        Assert.Contains("42", _stdout.ToString());
    }

    [Fact]
    public void WriteResult_String_WritesValue()
    {
        OutputFormatter.WriteResult("hello world");

        Assert.Contains("hello world", _stdout.ToString());
    }

    [Fact]
    public void WriteResult_VoidTaskResult_WritesNothing()
    {
        var voidTaskResultType = typeof(Task)
            .Assembly.GetType("System.Threading.Tasks.VoidTaskResult");
        Assert.NotNull(voidTaskResultType);
        var instance = Activator.CreateInstance(voidTaskResultType);

        OutputFormatter.WriteResult(instance);

        Assert.Equal(string.Empty, _stdout.ToString());
    }

    [Fact]
    public void WriteCompilationErrors_WithErrors_WritesToStderr()
    {
        var tree = CSharpSyntaxTree.ParseText("invalid C# !!!");
        var compilation = CSharpCompilation.Create("Test", [tree]);
        var diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error);

        OutputFormatter.WriteCompilationErrors(diagnostics);

        var error = _stderr.ToString();
        Assert.Contains("Compilation errors:", error);
    }

    [Fact]
    public void WriteCompilationErrors_EmptyDiagnostics_WritesHeaderOnly()
    {
        OutputFormatter.WriteCompilationErrors([]);

        var error = _stderr.ToString();
        Assert.Contains("Compilation errors:", error);
    }

    [Fact]
    public void WriteRuntimeError_WithException_WritesMessageToStderr()
    {
        OutputFormatter.WriteRuntimeError(new Exception("test error"));

        var error = _stderr.ToString();
        Assert.Contains("Runtime error: test error", error);
    }

    [Fact]
    public void WriteRuntimeError_WithStackTrace_WritesStackTrace()
    {
        Exception ex;
        try { throw new InvalidOperationException("boom"); }
        catch (Exception e) { ex = e; }

        OutputFormatter.WriteRuntimeError(ex);

        var error = _stderr.ToString();
        Assert.Contains("Runtime error: boom", error);
        Assert.Contains("WriteRuntimeError_WithStackTrace_WritesStackTrace", error);
    }

    [Fact]
    public void WriteDebug_WritesPrefixedMessageToStderr()
    {
        OutputFormatter.WriteDebug("test msg");

        var error = _stderr.ToString();
        Assert.Contains("[DEBUG] test msg", error);
    }
}

[CollectionDefinition("ConsoleOutput", DisableParallelization = true)]
public class ConsoleOutputCollection;
