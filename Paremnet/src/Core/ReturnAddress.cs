using Paremnet.Data;

namespace Paremnet.Core;

/// <summary>
/// Stores continuation, for example so that we can resume execution after a function call
/// </summary>
public class ReturnAddress(Closure fn, int pc, Environment env, string debug)
{
    /// <summary> Closure we're returning to </summary>
    public readonly Closure fn = fn;

    /// <summary> Program counter we're returning to </summary>
    public readonly int pc = pc;

    /// <summary> Environment that needs to be restored </summary>
    public readonly Environment env = env;

    /// <summary> Return label name for debugging </summary>
    public readonly string debug = debug;
}