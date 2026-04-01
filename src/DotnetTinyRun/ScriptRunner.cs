using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace DotnetTinyRun;

public sealed class ScriptRunner
{
    private readonly ReferenceResolver _referenceResolver = new();
    private readonly ScriptPreprocessor _preprocessor = new();

    public async Task<int> RunAsync(ScriptRunOptions options)
    {
        string code;
        try
        {
            code = options.GetEffectiveCode();
        }
        catch (Exception ex)
        {
            OutputFormatter.WriteRuntimeError(ex);
            return 3;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            Console.Error.WriteLine("No code to execute.");
            return 3;
        }

        // Load project context if specified
        ProjectContext? projectContext = null;
        if (options.ProjectPath is not null)
        {
            try
            {
                var loader = new ProjectContextLoader();
                projectContext = await loader.LoadAsync(options.ProjectPath, options.Debug);

                // Add project's native library directories to search path
                // and set working directory to project output
                if (projectContext.TargetAssemblyPath is not null)
                {
                    var targetDir = Path.GetDirectoryName(projectContext.TargetAssemblyPath);
                    if (targetDir is not null)
                    {
                        AddNativeLibraryPaths(targetDir, options.Debug);

                        // Set working directory to project output so relative paths work
                        Directory.SetCurrentDirectory(targetDir);

                        // Register a fallback assembly resolver so that shared-framework assemblies
                        // (e.g. Microsoft.Extensions.*) can be found when their version differs
                        // from the one requested by project NuGet packages.
                        RegisterAssemblyFallbackResolver(targetDir, projectContext.AssemblySearchPaths, options.Debug);
                    }
                }
            }
            catch (Exception ex)
            {
                OutputFormatter.WriteRuntimeError(ex);
                return 3;
            }
        }

        // Build script options
        var scriptOptions = _referenceResolver.BuildScriptOptions(options, projectContext);

        if (options.Debug)
        {
            OutputFormatter.WriteDebug($"Code length: {code.Length} chars");
            OutputFormatter.WriteDebug($"Project: {options.ProjectPath ?? "(none)"}");
        }

        // Preprocess code
        var effectiveCode = _preprocessor.Preprocess(code);

        // Execute
        try
        {
            var result = await CSharpScript.EvaluateAsync<object?>(effectiveCode, scriptOptions);

            // Skip printing if code already uses .Dump() (it prints internally)
            if (!effectiveCode.Contains(".Dump("))
                OutputFormatter.WriteResult(result);
            return 0;
        }
        catch (CompilationErrorException ex)
        {
            OutputFormatter.WriteCompilationErrors(ex.Diagnostics);
            return 1;
        }
        catch (Exception ex)
        {
            OutputFormatter.WriteRuntimeError(ex);
            return 2;
        }
    }

    /// <summary>
    /// Adds the platform-appropriate native library directory from the project's runtimes folder
    /// to PATH (Windows) and LD_LIBRARY_PATH (Linux) / DYLD_LIBRARY_PATH (macOS).
    /// </summary>
    private static void AddNativeLibraryPaths(string targetDir, bool debug)
    {
        var runtimesDir = Path.Combine(targetDir, "runtimes");
        if (!Directory.Exists(runtimesDir)) return;

        // Determine platform-specific RID
        string rid;
        var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture
            .ToString().ToLowerInvariant();
        if (OperatingSystem.IsWindows())
            rid = $"win-{arch}";
        else if (OperatingSystem.IsLinux())
            rid = $"linux-{arch}";
        else if (OperatingSystem.IsMacOS())
            rid = $"osx-{arch}";
        else
            rid = $"linux-{arch}";

        var nativeDir = Path.Combine(runtimesDir, rid, "native");
        if (!Directory.Exists(nativeDir)) return;

        if (debug) OutputFormatter.WriteDebug($"Native library directory: {nativeDir}");

        // Update PATH / LD_LIBRARY_PATH for child process resolution
        if (OperatingSystem.IsWindows())
        {
            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            Environment.SetEnvironmentVariable("PATH", $"{nativeDir};{currentPath}");
        }
        else if (OperatingSystem.IsLinux())
        {
            var current = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";
            Environment.SetEnvironmentVariable("LD_LIBRARY_PATH",
                string.IsNullOrEmpty(current) ? nativeDir : $"{nativeDir}:{current}");
        }
        else if (OperatingSystem.IsMacOS())
        {
            var current = Environment.GetEnvironmentVariable("DYLD_LIBRARY_PATH") ?? "";
            Environment.SetEnvironmentVariable("DYLD_LIBRARY_PATH",
                string.IsNullOrEmpty(current) ? nativeDir : $"{nativeDir}:{current}");
        }

        // Pre-emptively load native libraries using NativeLibrary so DllImport can find them.
        // This is necessary because LD_LIBRARY_PATH changes don't affect already-running processes
        // on some platforms when the library hasn't been loaded yet via dlopen.
        RegisterNativeLibraryResolver(nativeDir, debug);
    }

    private static void RegisterNativeLibraryResolver(string nativeDir, bool debug)
    {
        // Pre-load all native libraries found in the directory so they are available
        // in the process-wide library cache when later DllImport calls request them.
        // This is needed because LD_LIBRARY_PATH changes don't affect the .NET runtime's
        // own native library probing paths (AppContext.BaseDirectory, etc.).
        foreach (var dll in Directory.GetFiles(nativeDir, "*"))
        {
            var ext = Path.GetExtension(dll).ToLowerInvariant();
            if (ext is ".so" or ".dylib" or ".dll")
            {
                // Also copy to the project output root so AppContext.BaseDirectory probing finds it
                var targetDir = Path.GetDirectoryName(nativeDir)!; // runtimes/rid
                targetDir = Path.GetDirectoryName(targetDir)!;    // runtimes
                targetDir = Path.GetDirectoryName(targetDir)!;    // output dir

                var destPath = Path.Combine(targetDir, Path.GetFileName(dll));
                if (!File.Exists(destPath))
                {
                    try
                    {
                        File.Copy(dll, destPath);
                        if (debug) OutputFormatter.WriteDebug($"Copied native lib: {Path.GetFileName(dll)} -> {targetDir}");
                    }
                    catch (Exception ex) when (debug)
                    {
                        OutputFormatter.WriteDebug($"Could not copy native lib {Path.GetFileName(dll)}: {ex.Message}");
                    }
                    catch { /* non-debug: copying is best-effort; NativeLibrary.Load below is the primary path */ }
                }

                try
                {
                    System.Runtime.InteropServices.NativeLibrary.Load(dll);
                    if (debug) OutputFormatter.WriteDebug($"Pre-loaded native lib: {Path.GetFileName(dll)}");
                }
                catch (Exception ex) when (debug)
                {
                    OutputFormatter.WriteDebug($"Could not pre-load native lib {Path.GetFileName(dll)}: {ex.Message}");
                }
                catch { /* non-debug: pre-loading is best-effort */ }
            }
        }
    }

    /// <summary>
    /// Registers a one-time fallback on <see cref="AssemblyLoadContext.Default"/> so that
    /// assemblies that cannot be found through the default probing (e.g. shared-framework
    /// assemblies whose version differs from the one requested by a NuGet package) are
    /// resolved by scanning the project output directory and any extra search paths.
    /// </summary>
    private static void RegisterAssemblyFallbackResolver(string targetDir, IReadOnlyList<string> extraSearchPaths, bool debug)
    {
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            // Search in the project output directory and any extra paths provided
            foreach (var searchDir in new[] { targetDir }.Concat(extraSearchPaths))
            {
                if (!Directory.Exists(searchDir)) continue;
                var candidate = Path.Combine(searchDir, $"{name.Name}.dll");
                if (File.Exists(candidate))
                {
                    try { return context.LoadFromAssemblyPath(candidate); }
                    catch (Exception ex) when (debug)
                    {
                        OutputFormatter.WriteDebug($"Fallback resolver: failed to load {candidate}: {ex.Message}");
                    }
                    catch { /* try next path */ }
                }
            }
            return null;
        };
    }
}
