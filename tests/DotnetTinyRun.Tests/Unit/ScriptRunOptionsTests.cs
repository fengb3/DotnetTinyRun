using Xunit;

namespace DotnetTinyRun.Tests.Unit;

public class ScriptRunOptionsTests
{
    [Fact]
    public void GetEffectiveCode_WithCode_ReturnsCode()
    {
        var options = new ScriptRunOptions { Code = "1+2" };

        var code = options.GetEffectiveCode();

        Assert.Equal("1+2", code);
    }

    [Fact]
    public void GetEffectiveCode_CodeTakesPrecedenceOverFilePath()
    {
        var options = new ScriptRunOptions { Code = "from code", FilePath = "some file" };

        var code = options.GetEffectiveCode();

        Assert.Equal("from code", code);
    }

    [Fact]
    public void GetEffectiveCode_WithFilePath_ReadsFileContent()
    {
        var testFile = Path.Combine(AppContext.BaseDirectory, "TestData", "simple.csx");
        var options = new ScriptRunOptions { FilePath = testFile };

        var code = options.GetEffectiveCode();

        Assert.Equal("1 + 1", code.Trim());
    }

    [Fact]
    public void GetEffectiveCode_WithNullCodeAndNullFileAndNoStdin_ThrowsInvalidOperation()
    {
        // This test only passes when stdin is not redirected
        if (Console.IsInputRedirected)
            return;

        var options = new ScriptRunOptions();

        Assert.Throws<InvalidOperationException>(() => options.GetEffectiveCode());
    }

    [Fact]
    public void GetEffectiveCode_NonExistentFile_ThrowsFileNotFound()
    {
        var options = new ScriptRunOptions { FilePath = "nonexistent.csx" };

        Assert.Throws<FileNotFoundException>(() => options.GetEffectiveCode());
    }
}
