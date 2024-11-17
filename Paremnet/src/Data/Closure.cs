using Paremnet.Core;

namespace Paremnet.Data;

/// <summary>
/// Encapsulates everything needed for a function call
/// </summary>
public class Closure
{
    /// <summary> Handle to the compiled sequence of instructions </summary>
    public readonly CodeHandle code;

    /// <summary> Environment in which we're running </summary>
    public readonly Environment env;

    /// <summary> List of arguments this function expects </summary>
    public readonly Cons args;

    /// <summary> Optional closure name, for debugging purposes only </summary>
    public readonly string name;

    public Closure(CodeHandle code, Environment env, Cons args, string name)
    {
        this.code = code;
        this.env = env;
        this.args = args;
        this.name = name;
    }
}