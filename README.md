# DotnetTinyRun

A lightweight CLI tool for running C# scripts — like a CLI version of LINQPad.

Powered by [Roslyn](https://github.com/dotnet/roslyn), it supports inline code, script files, stdin piping, and can load full project context (references, using directives, namespaces) from any `.csproj`.

## Install

```bash
dotnet tool install --global DotnetTinyRun
```

## Usage

```bash
# Inline code
dotnet-tinyrun "1 + 2"
dotnet-tinyrun "DateTime.Now.ToString(\"o\")"

# LINQPad-style .Dump()
dotnet-tinyrun "new[] { \"apple\", \"banana\", \"cherry\" }.Dump()"
dotnet-tinyrun "Enumerable.Range(1, 10).Where(x => x % 2 == 0).Dump()"

# Load project context (references, usings, namespaces)
dotnet-tinyrun -p ./src/MyApp/MyApp.csproj "DbContext.Users.Count()"

# Run a .csx script file
dotnet-tinyrun -f script.csx

# Pipe via stdin
echo "DateTime.Now" | dotnet-tinyrun
```

## Options

```
Arguments:
  CODE                      Inline C# code to execute

Options:
  -p, --project <PATH>      Path to a .csproj to load context from
  -f, --file <PATH>         Path to a .csx file containing code
  -u, --using <NS>          Additional using directive (repeatable)
  -r, --reference <PATH>    Additional assembly reference (repeatable)
      --no-default-imports  Don't add default using directives
      --debug               Print debug information
  -h, --help                Show help
```

## Features

### Default Imports

The following namespaces are imported by default, so you can use them without explicit `using` statements:

`System`, `System.Linq`, `System.Collections`, `System.Collections.Generic`, `System.IO`, `System.Net.Http`, `System.Threading.Tasks`, `System.Text`, `System.Text.Json`

### .Dump() Extension

A `Dump()` extension method is available on any object, similar to LINQPad:

```csharp
// Print and return a value
42.Dump()
"hello".Dump("label")

// Dump collections
new[] { 1, 2, 3 }.Dump()
```

### Project Context

With `--project`, DotnetTinyRun will:

1. Build the project
2. Resolve all NuGet and project dependencies
3. Extract `using` directives from the `.csproj` and `GlobalUsings.cs`
4. Auto-discover namespaces from the project's output assembly
5. Set the working directory to the project output path

This lets you run one-liners against your project's full type system:

```bash
dotnet-tinyrun -p ./MyWebApp/MyWebApp.csproj "new HttpClient().GetStringAsync(\"https://example.com\").Result.Length"
```

## Build from Source

```bash
git clone https://github.com/fengb3/DotnetTinyRun.git
cd DotnetTinyRun
dotnet build src/DotnetTinyRun
```

## License

MIT
