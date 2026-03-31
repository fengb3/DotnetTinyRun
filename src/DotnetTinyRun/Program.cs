namespace DotnetTinyRun;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var options = ArgumentParser.Parse(args);
        var runner = new ScriptRunner();
        return await runner.RunAsync(options);
    }
}
