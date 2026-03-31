using System.Reflection;
using Microsoft.CodeAnalysis;

namespace DotnetTinyRun;

public static class OutputFormatter
{
    public static void WriteResult(object? result)
    {
        if (result is null)
            return;

        var typeName = result.GetType().FullName;

        // Don't print for void-like results
        if (typeName == "System.Threading.Tasks.VoidTaskResult")
            return;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(result);
        Console.ResetColor();
    }

    public static void WriteCompilationErrors(IEnumerable<Diagnostic> diagnostics)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("Compilation errors:");
        foreach (var diag in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
        {
            var loc = diag.Location.GetLineSpan();
            var line = loc.IsValid ? $" (line {loc.StartLinePosition.Line + 1})" : "";
            Console.Error.WriteLine($"  {diag.Id}{line}: {diag.GetMessage()}");
        }
        Console.ResetColor();
    }

    public static void WriteRuntimeError(Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Runtime error: {ex.Message}");
        if (ex.StackTrace is not null)
            Console.Error.WriteLine(ex.StackTrace);
        Console.ResetColor();
    }

    public static void WriteDebug(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Error.WriteLine($"[DEBUG] {message}");
        Console.ResetColor();
    }
}
