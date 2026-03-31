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
                        var nativeDir = Path.Combine(targetDir, "runtimes", "win-x64", "native");
                        if (Directory.Exists(nativeDir))
                        {
                            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                            Environment.SetEnvironmentVariable("PATH", $"{nativeDir};{currentPath}");
                        }

                        // Set working directory to project output so relative paths work
                        Directory.SetCurrentDirectory(targetDir);
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
}
