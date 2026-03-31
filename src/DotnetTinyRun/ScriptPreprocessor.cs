namespace DotnetTinyRun;

public sealed class ScriptPreprocessor
{
    public string Preprocess(string code)
    {
        // No transformation needed - Dump() is provided via in-memory helpers assembly
        return code;
    }
}
