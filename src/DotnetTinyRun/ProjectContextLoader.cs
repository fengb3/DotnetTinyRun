using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace DotnetTinyRun;

public sealed class ProjectContext
{
    public IReadOnlyList<MetadataReference> MetadataReferences { get; init; } = [];
    public IReadOnlyList<string> UsingDirectives { get; init; } = [];
    public string TargetAssemblyPath { get; init; } = "";
}

public sealed partial class ProjectContextLoader
{
    public async Task<ProjectContext> LoadAsync(string projectPath, bool debug)
    {
        if (!File.Exists(projectPath))
            throw new FileNotFoundException($"Project file not found: {projectPath}");

        var fullPath = Path.GetFullPath(projectPath);

        // Ensure the project is built
        await RunDotnetAsync(fullPath, "build", debug);

        // Get output assembly path
        var targetPathOutput = await RunDotnetAsync(fullPath, "msbuild -getProperty:TargetPath", debug);
        var targetAssemblyPath = ParsePropertyFromJson(targetPathOutput, "TargetPath");

        if (string.IsNullOrEmpty(targetAssemblyPath) || !File.Exists(targetAssemblyPath))
            throw new InvalidOperationException($"Could not find output assembly for {projectPath}");

        // Extract using directives
        var usings = await ExtractUsingDirectivesAsync(fullPath);

        // Resolve dependencies using AssemblyDependencyResolver
        var metadataReferences = ResolveDependencies(targetAssemblyPath, debug);

        // Auto-discover namespaces from the project's output assembly
        var namespaces = ExtractNamespaces(targetAssemblyPath);
        usings.AddRange(namespaces);

        if (debug)
        {
            OutputFormatter.WriteDebug($"Target assembly: {targetAssemblyPath}");
            OutputFormatter.WriteDebug($"Resolved {metadataReferences.Count} dependency references");
            OutputFormatter.WriteDebug($"Using directives: {string.Join(", ", usings)}");
        }

        return new ProjectContext
        {
            MetadataReferences = metadataReferences,
            UsingDirectives = usings,
            TargetAssemblyPath = targetAssemblyPath,
        };
    }

    private static List<MetadataReference> ResolveDependencies(string targetAssemblyPath, bool debug)
    {
        var references = new List<MetadataReference>();
        var targetDir = Path.GetDirectoryName(targetAssemblyPath)!;
        var resolver = new AssemblyDependencyResolver(targetAssemblyPath);

        // Load the target assembly to get its referenced assemblies
        var alc = new AssemblyLoadContext("TinyRun.ProjectContext", isCollectible: true);
        try
        {
            var assembly = alc.LoadFromAssemblyPath(targetAssemblyPath);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<AssemblyName>(assembly.GetReferencedAssemblies());

            while (queue.Count > 0)
            {
                var asmName = queue.Dequeue();
                if (!visited.Add(asmName.Name ?? ""))
                    continue;

                var resolvedPath = resolver.ResolveAssemblyToPath(asmName);
                if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
                    continue;

                try
                {
                    references.Add(MetadataReference.CreateFromFile(resolvedPath));

                    // Walk transitive dependencies
                    var refAssembly = alc.LoadFromAssemblyPath(resolvedPath);
                    foreach (var dep in refAssembly.GetReferencedAssemblies())
                    {
                        if (!visited.Contains(dep.Name ?? ""))
                            queue.Enqueue(dep);
                    }
                }
                catch (Exception ex) when (debug)
                {
                    OutputFormatter.WriteDebug($"Failed to load: {resolvedPath} - {ex.Message}");
                }
            }

            // Add the project's own assembly
            references.Add(MetadataReference.CreateFromFile(targetAssemblyPath));
        }
        finally
        {
            alc.Unload();
        }

        return references;
    }

    private static string ParsePropertyFromJson(string output, string propertyName)
    {
        try
        {
            using var doc = JsonDocument.Parse(output);
            if (doc.RootElement.TryGetProperty("Properties", out var props) &&
                props.TryGetProperty(propertyName, out var prop))
            {
                return prop.GetString() ?? "";
            }
        }
        catch (JsonException) { }

        // Fallback: treat as plain text
        return output.Trim();
    }

    private static List<string> ExtractNamespaces(string assemblyPath)
    {
        var namespaces = new HashSet<string>();
        try
        {
            var resolver = new AssemblyDependencyResolver(assemblyPath);
            var alc = new AssemblyLoadContext("TinyRun.NamespaceScan", isCollectible: true);
            alc.Resolving += (context, name) =>
            {
                var path = resolver.ResolveAssemblyToPath(name);
                return path is not null ? context.LoadFromAssemblyPath(path) : null;
            };

            try
            {
                var assembly = alc.LoadFromAssemblyPath(assemblyPath);
                foreach (var type in assembly.GetTypes())
                {
                    var ns = type.Namespace;
                    if (!string.IsNullOrEmpty(ns))
                        namespaces.Add(ns);
                }
            }
            finally
            {
                alc.Unload();
            }
        }
        catch { }

        return namespaces.ToList();
    }

    private static async Task<string> RunDotnetAsync(string projectPath, string arguments, bool debug)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{arguments} \"{projectPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (debug)
            OutputFormatter.WriteDebug($"Running: dotnet {arguments} \"{projectPath}\"");

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start 'dotnet {arguments}'");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (debug && !string.IsNullOrEmpty(error))
            OutputFormatter.WriteDebug($"stderr: {error}");

        return output;
    }

    private static async Task<List<string>> ExtractUsingDirectivesAsync(string projectPath)
    {
        var usings = new List<string>();

        try
        {
            var doc = XDocument.Load(projectPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            foreach (var elem in doc.Descendants(ns + "Using"))
            {
                var include = elem.Attribute("Include")?.Value;
                if (include is not null)
                    usings.Add(include);
            }

            var implicitUsings = doc.Descendants(ns + "ImplicitUsings").FirstOrDefault();
            if (implicitUsings?.Value.Equals("enable", StringComparison.OrdinalIgnoreCase) == true)
            {
                usings.AddRange(GetSdkDefaultUsings());
            }
        }
        catch (Exception) { }

        var projectDir = Path.GetDirectoryName(projectPath)!;
        var globalUsingsPath = Path.Combine(projectDir, "GlobalUsings.cs");
        if (File.Exists(globalUsingsPath))
        {
            var lines = await File.ReadAllLinesAsync(globalUsingsPath);
            foreach (var line in lines)
            {
                var match = GlobalUsingRegex().Match(line);
                if (match.Success)
                    usings.Add(match.Groups[1].Value);
            }
        }

        return usings.Distinct().ToList();
    }

    private static IReadOnlyList<string> GetSdkDefaultUsings()
    {
        return
        [
            "System",
            "System.Collections.Generic",
            "System.IO",
            "System.Linq",
            "System.Net.Http",
            "System.Threading",
            "System.Threading.Tasks",
        ];
    }

    [GeneratedRegex(@"global\s+using\s+(\w[\w.]*)")]
    private static partial Regex GlobalUsingRegex();
}
