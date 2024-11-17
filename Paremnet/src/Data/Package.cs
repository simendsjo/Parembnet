using Paremnet.Error;
using System.Collections.Generic;
using System.Linq;

namespace Paremnet.Data;

/// <summary>
/// Package is a storage for symbols. When the parser reads out symbols from the stream,
/// it retrieves the appropriate symbol from the package, or if one hasn't been seen before,
/// it interns a new one.
/// </summary>
public class Package(string name)
{
    /// <summary> Name of this package </summary>
    public readonly string Name = name;

    /// <summary> Map from symbol name (string) to instance (Symbol) </summary>
    private readonly Dictionary<string, Symbol> _symbols = new();

    /// <summary> Map from symbol (Symbol) to its value (*) </summary>
    private readonly Dictionary<Symbol, Val> _bindings = new();

    /// <summary> Map from macro name (Symbol) to the actual macro body </summary>
    private readonly Dictionary<Symbol, Macro> _macros = new();

    /// <summary> 
    /// Vector of other packages imported into this one. 
    /// Symbol lookup will use these packages, if the symbol is not found here. 
    /// </summary>
    private readonly List<Package> _imports = new();

    /// <summary> 
    /// Returns a symbol with the given name if one was interned, undefined otherwise.
    /// If deep is true, it will also search through all packages imported by this one. 
    /// </summary>
    public Symbol Find(string name, bool deep)
    {
        if (_symbols.TryGetValue(name, out Symbol result))
        {
            return result;
        }

        if (deep)
        {
            foreach (Package pkg in _imports)
            {
                result = pkg.Find(name, deep);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    /// <summary> 
    /// Interns the given name. If a symbol with this name already exists, it is returned.
    /// Otherwise a new symbol is created, added to internal storage, and returned.
    /// </summary>
    public Symbol Intern(string name)
    {
        if (!_symbols.TryGetValue(name, out Symbol result))
        {
            result = _symbols[name] = new Symbol(name, this);
        }
        return result;
    }


    /// <summary> 
    /// Uninterns the given symbol. If a symbol existed with this name, it will be removed,
    /// and the function returns true; otherwise returns false.
    /// </summary>
    public bool Unintern(string name)
    {
        if (_symbols.ContainsKey(name))
        {
            _symbols.Remove(name);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary> Retrieves the value binding for the given symbol, also traversing the import list. </summary>
    public Val GetValue(Symbol symbol)
    {
        if (symbol.Pkg != this)
        {
            throw new LanguageError("Unexpected package in getBinding: " + symbol.Pkg.Name);
        }

        if (_bindings.TryGetValue(symbol, out Val val))
        {
            return val;
        }

        // try imports
        foreach (Package pkg in _imports)
        {
            Symbol local = pkg.Find(symbol.Name, false);
            if (local != null && local.Exported)
            {
                if (pkg._bindings.TryGetValue(local, out Val innerval))
                {
                    return innerval;
                }
            }
        }

        return Val.NIL;
    }

    /// <summary> Sets the binding for the given symbol. If NIL, deletes the binding. </summary>
    public void SetValue(Symbol symbol, Val value)
    {
        if (symbol.Pkg != this)
        {
            throw new LanguageError("Unexpected package in setBinding: " + symbol.Pkg.Name);
        }

        if (value.IsNil)
        {
            _bindings.Remove(symbol);
        }
        else
        {
            _bindings[symbol] = value;
        }
    }

    /// <summary> Returns true if this package contains the named macro </summary>
    public bool HasMacro(Symbol symbol) => GetMacro(symbol) != null;

    /// <summary> Retrieves the macro for the given symbol, potentially null </summary>
    public Macro GetMacro(Symbol symbol)
    {
        if (symbol.Pkg != this)
        {
            throw new LanguageError("Unexpected package in getBinding: " + symbol.Pkg.Name);
        }

        if (_macros.TryGetValue(symbol, out Macro val))
        {
            return val;
        }

        // try imports
        foreach (Package pkg in _imports)
        {
            Symbol s = pkg.Find(symbol.Name, false);
            if (s != null && s.Exported)
            {
                if (pkg._macros.TryGetValue(s, out Macro innerval))
                {
                    return innerval;
                }
            }
        }

        return null;
    }

    /// <summary> Sets the macro for the given symbol. If null, deletes the macro. </summary>
    public void SetMacro(Symbol symbol, Macro value)
    {
        if (symbol.Pkg != this)
        {
            throw new LanguageError("setMacro called with invalid package");
        }

        if (value == null)
        {
            _macros.Remove(symbol);
        }
        else
        {
            _macros[symbol] = value;
        }
    }

    /// <summary> Adds a new import </summary>
    public void AddImport(Package pkg)
    {
        if (pkg == this)
        {
            throw new LanguageError("Package cannot import itself!");
        }

        if (!_imports.Contains(pkg))
        {
            _imports.Add(pkg);
        }
    }

    /// <summary> Returns a new vector of all symbols imported by this package </summary>
    public List<Val> ListImports() => _imports.Select(pkg => new Val(pkg.Name)).ToList();

    /// <summary> Returns a new vector of all symbols interned and exported by this package </summary>
    public List<Val> ListExports() => _symbols.Values.Where(sym => sym.Exported).Select(sym => new Val(sym)).ToList();
}