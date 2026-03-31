namespace DotnetTinyRun;

public sealed class ScriptRunOptions
{
    public string? Code { get; init; }
    public string? FilePath { get; init; }
    public string? ProjectPath { get; init; }
    public List<string> AdditionalUsings { get; init; } = [];
    public List<string> AdditionalReferences { get; init; } = [];
    public bool NoDefaultImports { get; init; }
    public bool Debug { get; init; }

    public string GetEffectiveCode()
    {
        if (Code is not null)
            return Code;

        if (FilePath is not null)
            return File.ReadAllText(FilePath);

        if (Console.IsInputRedirected)
            return Console.In.ReadToEnd();

        throw new InvalidOperationException("No code provided. Pass inline code, use --file, or pipe via stdin.");
    }
}

public static class ArgumentParser
{
    public static ScriptRunOptions Parse(string[] args)
    {
        var options = new ScriptRunOptions();
        var code = new List<string>();
        var usings = new List<string>();
        var references = new List<string>();
        string? project = null;
        string? file = null;
        bool noDefaultImports = false;
        bool debug = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-p" or "--project":
                    project = NextArg(args, ref i, "--project");
                    break;
                case "-f" or "--file":
                    file = NextArg(args, ref i, "--file");
                    break;
                case "-u" or "--using":
                    usings.Add(NextArg(args, ref i, "--using"));
                    break;
                case "-r" or "--reference":
                    references.Add(NextArg(args, ref i, "--reference"));
                    break;
                case "--no-default-imports":
                    noDefaultImports = true;
                    break;
                case "--debug":
                    debug = true;
                    break;
                case "-h" or "--help":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
                default:
                    code.Add(args[i]);
                    break;
            }
        }

        return new ScriptRunOptions
        {
            Code = code.Count > 0 ? string.Join(" ", code) : null,
            FilePath = file,
            ProjectPath = project,
            AdditionalUsings = usings,
            AdditionalReferences = references,
            NoDefaultImports = noDefaultImports,
            Debug = debug,
        };
    }

    private static string NextArg(string[] args, ref int i, string option)
    {
        if (i + 1 >= args.Length)
            throw new InvalidOperationException($"Option '{option}' requires a value.");
        return args[++i];
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            DotnetTinyRun - CLI version of LINQPad

            Usage: dotnet-tinyrun [CODE] [OPTIONS]

            Arguments:
              CODE                    Inline C# code to execute

            Options:
              -p, --project <PATH>    Path to a .csproj to load context from
              -f, --file <PATH>       Path to a .csx file containing code
              -u, --using <NS>        Additional using directive (repeatable)
              -r, --reference <PATH>  Additional assembly reference (repeatable)
                  --no-default-imports  Don't add default using directives
                  --debug              Print debug information
              -h, --help              Show this help

            Examples:
              dotnet-tinyrun "1 + 2"
              dotnet-tinyrun "new[] {1,2,3}.Dump()"
              dotnet-tinyrun -p ./MyApp/MyApp.csproj "DbContext.Users.Count()"
              echo "DateTime.Now" | dotnet-tinyrun
            """);
    }
}
