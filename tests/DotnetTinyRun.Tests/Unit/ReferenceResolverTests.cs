using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Xunit;

namespace DotnetTinyRun.Tests.Unit;

public class ReferenceResolverTests
{
    private readonly ReferenceResolver _resolver = new();

    [Fact]
    public async Task BuildScriptOptions_DefaultImports_IncludesSystemLinq()
    {
        var options = new ScriptRunOptions();
        var scriptOptions = _resolver.BuildScriptOptions(options, null);

        var result = await CSharpScript.EvaluateAsync<int>(
            "new[] {1,2,3}.Select(x => x * 2).Sum()", scriptOptions);

        Assert.Equal(12, result);
    }

    [Fact]
    public async Task BuildScriptOptions_NoDefaultImports_LinqNotAvailable()
    {
        var options = new ScriptRunOptions { NoDefaultImports = true };
        var scriptOptions = _resolver.BuildScriptOptions(options, null);

        await Assert.ThrowsAsync<CompilationErrorException>(() =>
            CSharpScript.EvaluateAsync<int>(
                "new[] {1,2,3}.Select(x => x * 2).Sum()", scriptOptions));
    }

    [Fact]
    public async Task BuildScriptOptions_DumpExtensionWorks()
    {
        var options = new ScriptRunOptions();
        var scriptOptions = _resolver.BuildScriptOptions(options, null);

        var result = await CSharpScript.EvaluateAsync<int>("42.Dump()", scriptOptions);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task BuildScriptOptions_AdditionalUsings_AreAvailable()
    {
        var options = new ScriptRunOptions { AdditionalUsings = ["System.IO"] };
        var scriptOptions = _resolver.BuildScriptOptions(options, null);

        // Path is in System.IO — should compile without explicit using
        var result = await CSharpScript.EvaluateAsync<char>(
            "Path.DirectorySeparatorChar", scriptOptions);

        Assert.Equal(Path.DirectorySeparatorChar, result);
    }

    [Fact]
    public async Task BuildScriptOptions_CalledTwice_BothSucceed()
    {
        var options = new ScriptRunOptions();
        var scriptOptions1 = _resolver.BuildScriptOptions(options, null);
        var scriptOptions2 = _resolver.BuildScriptOptions(options, null);

        var result1 = await CSharpScript.EvaluateAsync<int>("1 + 1", scriptOptions1);
        var result2 = await CSharpScript.EvaluateAsync<int>("2 + 2", scriptOptions2);

        Assert.Equal(2, result1);
        Assert.Equal(4, result2);
    }
}
