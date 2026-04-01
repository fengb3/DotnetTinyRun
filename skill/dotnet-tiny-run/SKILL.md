---
name: dotnet-tiny-run
description: >
  Run and verify C# code snippets instantly without creating a project. Use this skill whenever
  the user wants to quickly test, verify, or run a piece of C# code — whether it's a single
  expression, a LINQ query, a quick sanity check, or trying out an API. Also use it when the
  user finishes writing code in a .NET project and wants to validate a result without building
  the whole solution, or when they say things like "let me test this", "run this snippet",
  "does this work", "what does this return", "quick check", or want to run C# one-liners
  ad-hoc. Triggers on any request to execute or evaluate C# code outside of a formal test
  or project build workflow.
---

# DotnetTinyRun — Instant C# Script Runner

This skill uses [DotnetTinyRun](https://www.nuget.org/packages/DotnetTinyRun), a lightweight .NET global tool powered by Roslyn that runs C# code directly — no csproj needed. Think of it as a CLI version of LINQPad.

## Prerequisites Check

Before first use, verify the tool is installed:

```bash
dotnet-tiny-run --help
```

If this fails (command not found), install it globally:

```bash
dotnet tool install --global DotnetTinyRun
```

After installation, run `dotnet-tiny-run --help` again to confirm it's available and to see the full list of options.

## When to Use This Tool

This tool is ideal when the user needs to:

- **Verify a code snippet** — run a C# expression or block to see its output immediately
- **Validate logic after writing code** — test a function or expression against a project's types without writing a unit test
- **Explore an API or library** — try out methods, constructors, or LINQ queries interactively
- **Debug a calculation** — evaluate an expression with real values to check correctness

Do NOT use this as a replacement for:
- Proper unit tests (use `dotnet test` instead)
- Building or running a full application (use `dotnet run`)
- Long-running scripts that need their own project structure

## How to Run Code

The command name is `dotnet-tiny-run`. Pass C# code directly as a string argument:

```bash
dotnet-tiny-run "1 + 2"
```

### Default Imports

These namespaces are imported by default, so no `using` statements needed for common operations:

`System`, `System.Linq`, `System.Collections`, `System.Collections.Generic`, `System.IO`, `System.Net.Http`, `System.Threading.Tasks`, `System.Text`, `System.Text.Json`

### Output with .Dump()

Any object has a `.Dump()` extension method (like LINQPad) that prints the value to stdout:

```bash
dotnet-tiny-run "new[] { 1, 2, 3 }.Where(x => x > 1).Dump()"
```

### Loading Project Context

When the user wants to test code against an existing project (its types, NuGet packages, usings), use the `--project` flag:

```bash
dotnet-tiny-run -p ./src/MyApp/MyApp.csproj "SomeProjectType.SomeMethod().Dump()"
```

This builds the project, resolves all dependencies, and makes the project's types available in the snippet.

### Additional Options

- `-u, --using <NS>` — add extra using directives (repeatable)
- `-r, --reference <PATH>` — add assembly references (repeatable)
- `-f, --file <PATH>` — run a `.csx` script file instead of inline code
- `--no-default-imports` — skip the default using directives
- `--debug` — print debug information if something goes wrong

## Tips for the Agent

- For simple expressions, just pass the code as-is. You don't need `Console.WriteLine` — the expression result is printed automatically, and `.Dump()` gives formatted output.
- When testing against a project, always use `--project` so the snippet has access to the project's types and packages.
- If the code contains quotes or special characters, make sure to escape them properly for the shell.
- If execution fails, try `--debug` to see what's going on (compilation errors, missing references, etc.).
- For multi-statement logic, consider writing a `.csx` file and using `-f` instead of cramming everything into one line.
