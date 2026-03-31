using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;

namespace DotnetTinyRun;

public sealed class ReferenceResolver
{
    private MetadataReference? _helpersReference;

    public ScriptOptions BuildScriptOptions(ScriptRunOptions options, ProjectContext? projectContext)
    {
        var scriptOpts = ScriptOptions.Default;

        // Add core runtime references first
        foreach (var reference in GetCoreReferences())
            scriptOpts = scriptOpts.AddReferences(reference);

        // Add in-memory helpers assembly with Dump() extension method
        _helpersReference ??= CompileHelpersAssembly();
        scriptOpts = scriptOpts
            .AddReferences(_helpersReference)
            .AddImports("DotnetTinyRun.Helpers");

        // Add default imports
        if (!options.NoDefaultImports)
            scriptOpts = scriptOpts.AddImports(GetDefaultImports());

        // Add project context
        if (projectContext is not null)
        {
            // Only add project references that don't conflict with core runtime
            var coreRefPaths = GetCoreAssemblyPaths().ToHashSet();
            var filteredRefs = projectContext.MetadataReferences
                .Where(r => r.Display is null || !coreRefPaths.Contains(r.Display))
                .ToList();

            scriptOpts = scriptOpts
                .AddReferences(filteredRefs)
                .AddImports(projectContext.UsingDirectives);
        }

        // Add user-specified extras
        foreach (var refPath in options.AdditionalReferences)
        {
            if (File.Exists(refPath))
                scriptOpts = scriptOpts.AddReferences(MetadataReference.CreateFromFile(refPath));
        }

        scriptOpts = scriptOpts.AddImports(options.AdditionalUsings);

        return scriptOpts;
    }

    private static HashSet<string> GetCoreAssemblyPaths()
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        return Directory.GetFiles(runtimeDir, "*.dll")
            .Select(f => Path.GetFileName(f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<MetadataReference> GetCoreReferences()
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        // Essential references for any C# script
        var coreDlls = new[]
        {
            "System.Private.CoreLib.dll",
            "System.Runtime.dll",
            "System.Console.dll",
            "System.Linq.dll",
            "System.Collections.dll",
            "System.IO.FileSystem.dll",
            "System.Net.Http.dll",
            "System.Text.Json.dll",
            "System.Threading.Tasks.dll",
            "System.Text.RegularExpressions.dll",
            "System.ObjectModel.dll",
            "System.ComponentModel.dll",
        };

        foreach (var dll in coreDlls)
        {
            var path = Path.Combine(runtimeDir, dll);
            if (File.Exists(path))
                yield return MetadataReference.CreateFromFile(path);
        }
    }

    private static MetadataReference CompileHelpersAssembly()
    {
        var source = """
            using System;
            using System.Collections;

            namespace DotnetTinyRun.Helpers
            {
                public static class DumpExtensions
                {
                    public static T Dump<T>(this T obj, string? label = null)
                    {
                        if (label is not null)
                            Console.Write(label + ": ");
                        if (obj is null)
                            Console.WriteLine("null");
                        else if (obj is IEnumerable seq && obj is not string)
                        {
                            foreach (var item in seq)
                                Console.WriteLine(item);
                        }
                        else
                            Console.WriteLine(obj);
                        return obj;
                    }
                }
            }
            """;

        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var referencePaths = new[]
        {
            Path.Combine(runtimeDir, "System.Private.CoreLib.dll"),
            Path.Combine(runtimeDir, "System.Runtime.dll"),
            Path.Combine(runtimeDir, "System.Console.dll"),
            Path.Combine(runtimeDir, "System.Collections.dll"),
        }
        .Where(File.Exists)
        .Distinct();

        var references = referencePaths.Select(p => MetadataReference.CreateFromFile(p));

        var compilation = CSharpCompilation.Create(
            "DotnetTinyRunHelpers",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);
        if (!emitResult.Success)
        {
            var errors = string.Join("\n", emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString()));
            throw new InvalidOperationException($"Failed to compile helpers assembly:\n{errors}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        AssemblyLoadContext.Default.LoadFromStream(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return MetadataReference.CreateFromStream(ms);
    }

    private static IEnumerable<string> GetDefaultImports()
    {
        return
        [
            "System",
            "System.Linq",
            "System.Collections",
            "System.Collections.Generic",
            "System.IO",
            "System.Net.Http",
            "System.Threading.Tasks",
            "System.Text",
            "System.Text.Json",
        ];
    }
}
