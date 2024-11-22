namespace Paremnet.Data;

/// <summary>
/// Encapsulates a macro and code that runs to expand it.
/// </summary>
public class Macro(Symbol name, Cons args, Closure body)
{
    /// <summary> Optional debug name </summary>
    public readonly string Name = name.Name;

    /// <summary> List of arguments for the macro </summary>
    public readonly Cons Args = args;

    /// <summary> Body of the macro </summary>
    public readonly Closure Body = body;
}