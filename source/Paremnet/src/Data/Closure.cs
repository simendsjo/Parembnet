using Paremnet.Core;

namespace Paremnet.Data;

/// <summary>
/// Encapsulates everything needed for a function call
/// </summary>
public class Closure(CodeHandle code, Environment env, Cons args, string name)
{
    /// <summary> Handle to the compiled sequence of instructions </summary>
    public readonly CodeHandle Code = code;

    /// <summary> Environment in which we're running </summary>
    public readonly Environment Env = env;

    /// <summary> List of arguments this function expects </summary>
    public readonly Cons Args = args;

    /// <summary> Optional closure name, for debugging purposes only </summary>
    public readonly string Name = name;
}