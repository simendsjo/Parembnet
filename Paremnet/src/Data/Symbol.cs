using System.Diagnostics;

namespace Paremnet.Data;

/// <summary>
/// Immutable symbol, interned in a specific package.
/// Interned symbols are unique, so we can test for equality using simple ==
/// </summary>
[DebuggerDisplay("{DebugString}")]
public class Symbol(string name, Package pkg)
{
    /// <summary> String name of this symbol </summary>
    public string name { get; private set; } = name;

    /// <summary> Package in this symbol is interned </summary>
    public Package pkg { get; private set; } = pkg;

    /// <summary> Full (package-prefixed) name of this symbol </summary>
    public string fullName { get; private set; } = (pkg != null && pkg.name != null) ? (pkg.name + ":" + name) : name;

    /// <summary> If true, this symbol is visible outside of its package. This can be adjusted later. </summary>
    public bool exported = false;

    public override string ToString() => fullName;
    private string DebugString => fullName;
}