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
    /// <summary>Additional directories to search when resolving assemblies at runtime.</summary>
    public IReadOnlyList<string> AssemblySearchPaths { get; init; } = [];
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

        // Also include shared framework assemblies referenced in runtimeconfig.json
        var runtimeConfigPath = Path.ChangeExtension(targetAssemblyPath, ".runtimeconfig.json");
        var assemblySearchPaths = new List<string>();
        if (File.Exists(runtimeConfigPath))
        {
            var frameworkRefs = ResolveSharedFrameworkReferences(runtimeConfigPath, debug, out var frameworkDirs);
            assemblySearchPaths.AddRange(frameworkDirs);
            // Add only those not already covered by the project's own output
            var existing = metadataReferences.Select(r => Path.GetFileName(r.Display ?? ""))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var r in frameworkRefs)
            {
                if (!existing.Contains(Path.GetFileName(r.Display ?? "")))
                    metadataReferences.Add(r);
            }
        }

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
            AssemblySearchPaths = assemblySearchPaths,
        };
    }

    private static List<MetadataReference> ResolveSharedFrameworkReferences(string runtimeConfigPath, bool debug, out List<string> frameworkDirs)
    {
        var references = new List<MetadataReference>();
        frameworkDirs = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(runtimeConfigPath));
            var root = doc.RootElement;
            if (!root.TryGetProperty("runtimeOptions", out var runtimeOptions))
                return references;

            if (!runtimeOptions.TryGetProperty("frameworks", out var frameworks))
                return references;

            // Locate the dotnet shared directory next to the current runtime
            // e.g. /usr/share/dotnet/shared/Microsoft.NETCore.App/10.0.x -> /usr/share/dotnet/shared
            var coreLibDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            // coreLibDir is like /usr/share/dotnet/shared/Microsoft.NETCore.App/10.0.x
            var sharedDir = Path.GetFullPath(Path.Combine(coreLibDir, "..", ".."));

            foreach (var fw in frameworks.EnumerateArray())
            {
                if (!fw.TryGetProperty("name", out var nameElem) ||
                    !fw.TryGetProperty("version", out var versionElem))
                    continue;

                var fwName = nameElem.GetString();
                var fwVersion = versionElem.GetString();
                if (fwName is null || fwVersion is null) continue;

                // Skip the base runtime (already handled by GetCoreReferences in ReferenceResolver)
                if (fwName.Equals("Microsoft.NETCore.App", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Resolve best-matching installed version
                var fwDir = Path.Combine(sharedDir, fwName);
                if (!Directory.Exists(fwDir))
                {
                    if (debug) OutputFormatter.WriteDebug($"Shared framework directory not found: {fwDir}");
                    continue;
                }

                var resolvedDir = ResolveFrameworkVersion(fwDir, fwVersion);
                if (resolvedDir is null)
                {
                    if (debug) OutputFormatter.WriteDebug($"No compatible version of {fwName} found in {fwDir}");
                    continue;
                }

                if (debug) OutputFormatter.WriteDebug($"Loading shared framework: {fwName} from {resolvedDir}");
                frameworkDirs.Add(resolvedDir);

                foreach (var dll in Directory.GetFiles(resolvedDir, "*.dll"))
                {
                    try
                    {
                        references.Add(MetadataReference.CreateFromFile(dll));
                    }
                    catch (Exception ex) when (debug)
                    {
                        OutputFormatter.WriteDebug($"Could not load framework assembly {dll}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex) when (debug)
        {
            OutputFormatter.WriteDebug($"Failed to parse runtimeconfig: {ex.Message}");
        }

        return references;
    }

    /// <summary>
    /// Finds the best installed framework version that is compatible with the requested version.
    /// Prefers exact match, then highest patch within same major.minor.
    /// </summary>
    private static string? ResolveFrameworkVersion(string frameworkDir, string requestedVersion)
    {
        if (!Version.TryParse(requestedVersion, out var requested))
            return null;

        var installedVersions = Directory.GetDirectories(frameworkDir)
            .Select(d => (Path: d, Version: Version.TryParse(Path.GetFileName(d), out var v) ? v : null))
            .Where(x => x.Version is not null)
            .OrderByDescending(x => x.Version)
            .ToList();

        // Try exact match first
        var exact = installedVersions.FirstOrDefault(x => x.Version == requested);
        if (exact.Path is not null) return exact.Path;

        // Try same major.minor, highest patch
        var compatible = installedVersions
            .Where(x => x.Version!.Major == requested.Major && x.Version.Minor == requested.Minor)
            .FirstOrDefault();
        if (compatible.Path is not null) return compatible.Path;

        // Try same major, highest minor.patch
        var sameMajor = installedVersions
            .Where(x => x.Version!.Major == requested.Major)
            .FirstOrDefault();
        return sameMajor.Path;
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
