using Xunit;

namespace DotnetTinyRun.Tests.Unit;

public class ScriptPreprocessorTests
{
    [Fact]
    public void Preprocess_ReturnsCodeUnchanged()
    {
        var preprocessor = new ScriptPreprocessor();

        var result = preprocessor.Preprocess("var x = 1;");

        Assert.Equal("var x = 1;", result);
    }

    [Fact]
    public void Preprocess_EmptyString_ReturnsEmptyString()
    {
        var preprocessor = new ScriptPreprocessor();

        var result = preprocessor.Preprocess("");

        Assert.Equal("", result);
    }
}
