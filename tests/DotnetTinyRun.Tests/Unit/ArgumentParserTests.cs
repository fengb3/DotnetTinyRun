using Xunit;

namespace DotnetTinyRun.Tests.Unit;

public class ArgumentParserTests
{
    [Fact]
    public void Parse_InlineCode_ReturnsCode()
    {
        var options = ArgumentParser.Parse(["1 + 2"]);

        Assert.Equal("1 + 2", options.Code);
        Assert.Null(options.FilePath);
        Assert.Null(options.ProjectPath);
        Assert.Empty(options.AdditionalUsings);
        Assert.Empty(options.AdditionalReferences);
        Assert.False(options.NoDefaultImports);
        Assert.False(options.Debug);
    }

    [Fact]
    public void Parse_MultipleCodeArgs_JoinsWithSpace()
    {
        var options = ArgumentParser.Parse(["var x = 1;", "var y = 2;"]);

        Assert.Equal("var x = 1; var y = 2;", options.Code);
    }

    [Fact]
    public void Parse_ProjectOption_SetsProjectPath()
    {
        var options = ArgumentParser.Parse(["-p", "MyApp.csproj"]);

        Assert.Equal("MyApp.csproj", options.ProjectPath);
    }

    [Fact]
    public void Parse_LongProjectOption_SetsProjectPath()
    {
        var options = ArgumentParser.Parse(["--project", "MyApp.csproj"]);

        Assert.Equal("MyApp.csproj", options.ProjectPath);
    }

    [Fact]
    public void Parse_FileOption_SetsFilePath()
    {
        var options = ArgumentParser.Parse(["-f", "script.csx"]);

        Assert.Equal("script.csx", options.FilePath);
    }

    [Fact]
    public void Parse_LongFileOption_SetsFilePath()
    {
        var options = ArgumentParser.Parse(["--file", "script.csx"]);

        Assert.Equal("script.csx", options.FilePath);
    }

    [Fact]
    public void Parse_UsingOption_AddsToUsings()
    {
        var options = ArgumentParser.Parse(["-u", "System.Xml", "-u", "System.Net"]);

        Assert.Equal(["System.Xml", "System.Net"], options.AdditionalUsings);
    }

    [Fact]
    public void Parse_ReferenceOption_AddsToReferences()
    {
        var options = ArgumentParser.Parse(["-r", "lib.dll", "-r", "other.dll"]);

        Assert.Equal(["lib.dll", "other.dll"], options.AdditionalReferences);
    }

    [Fact]
    public void Parse_NoDefaultImportsFlag_SetsTrue()
    {
        var options = ArgumentParser.Parse(["--no-default-imports"]);

        Assert.True(options.NoDefaultImports);
    }

    [Fact]
    public void Parse_DebugFlag_SetsTrue()
    {
        var options = ArgumentParser.Parse(["--debug"]);

        Assert.True(options.Debug);
    }

    [Fact]
    public void Parse_AllOptionsTogether_ParsesCorrectly()
    {
        var options = ArgumentParser.Parse([
            "-p", "proj.csproj",
            "-u", "System.Xml",
            "-r", "lib.dll",
            "--no-default-imports",
            "--debug",
            "1+2"
        ]);

        Assert.Equal("1+2", options.Code);
        Assert.Equal("proj.csproj", options.ProjectPath);
        Assert.Equal(["System.Xml"], options.AdditionalUsings);
        Assert.Equal(["lib.dll"], options.AdditionalReferences);
        Assert.True(options.NoDefaultImports);
        Assert.True(options.Debug);
    }

    [Fact]
    public void Parse_EmptyArgs_ReturnsDefaultOptions()
    {
        var options = ArgumentParser.Parse([]);

        Assert.Null(options.Code);
        Assert.Null(options.FilePath);
        Assert.Null(options.ProjectPath);
        Assert.Empty(options.AdditionalUsings);
        Assert.Empty(options.AdditionalReferences);
        Assert.False(options.NoDefaultImports);
        Assert.False(options.Debug);
    }

    [Fact]
    public void Parse_ProjectOptionWithoutValue_ThrowsInvalidOperation()
    {
        Assert.Throws<InvalidOperationException>(() => ArgumentParser.Parse(["-p"]));
    }

    [Fact]
    public void Parse_FileOptionWithoutValue_ThrowsInvalidOperation()
    {
        Assert.Throws<InvalidOperationException>(() => ArgumentParser.Parse(["-f"]));
    }

    [Fact]
    public void Parse_UsingOptionWithoutValue_ThrowsInvalidOperation()
    {
        Assert.Throws<InvalidOperationException>(() => ArgumentParser.Parse(["-u"]));
    }

    [Fact]
    public void Parse_ReferenceOptionWithoutValue_ThrowsInvalidOperation()
    {
        Assert.Throws<InvalidOperationException>(() => ArgumentParser.Parse(["-r"]));
    }
}
