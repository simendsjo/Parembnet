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
    public string Name { get; private set; } = name;

    /// <summary> Package in this symbol is interned </summary>
    public Package Pkg { get; private set; } = pkg;

    /// <summary> Full (package-prefixed) name of this symbol </summary>
    public string FullName { get; private set; } = (pkg != null && pkg.Name != null) ? (pkg.Name + ":" + name) : name;

    /// <summary> If true, this symbol is visible outside of its package. This can be adjusted later. </summary>
    public bool Exported = false;

    public override string ToString() => FullName;
    private string DebugString => FullName;
}
