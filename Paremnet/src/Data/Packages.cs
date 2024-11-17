using System.Collections.Generic;

namespace Paremnet.Data;

/// <summary>
/// Packages class is responsible for managing the list of packages defined by the runtime.
/// </summary>
public class Packages
{
    private struct Entry
    {
        public string Name;
        public Package Package;
    }

    /// <summary> Global package, unnamed </summary>
    public static readonly string NameGlobal = null;

    /// <summary> Special keywords package </summary>
    public static readonly string NameKeywords = "";

    /// <summary> Core package with all the built-in primitives </summary>
    public static readonly string NameCore = "core";

    /// <summary> Dictionary of packages, keyed by package name </summary>
    private readonly List<Entry> _packages;

    /// <summary> Currently active package, used to intern symbols </summary>
    public Package Current;

    public Packages()
    {
        _packages = new List<Entry>();
        Reinitialize();
    }

    /// <summary> Helper function, returns the global package </summary>
    public Package Global => Find(NameGlobal);

    /// <summary> Helper function, returns the keywords package </summary>
    public Package Keywords => Find(NameKeywords);

    /// <summary> Helper function, returns the core package with all primitives </summary>
    public Package Core => Find(NameCore);

    /// <summary> Clears and re-initializes all packages to their initial settings. </summary>
    public void Reinitialize()
    {
        _packages.Clear();
        Add(new Package(NameCore));
        Add(new Package(NameGlobal));
        Add(new Package(NameKeywords));

        Global.AddImport(Core);
        Current = Global;
    }

    /// <summary> Finds index of the package in the list, or -1 if absent </summary>
    private int FindIndexOfPackage(string name)
    {
        for (int i = 0; i < _packages.Count; i++)
        {
            if (_packages[i].Name == name) { return i; }
        }
        return -1;
    }

    /// <summary> Finds a package by name (creating a new one if necessary) and returns it </summary>
    public Package Intern(string name)
    {
        Package pkg = Find(name);
        if (pkg == null)
        {
            pkg = Add(new Package(name));
            pkg.AddImport(Core); // every user package imports core
        }
        return pkg;
    }

    /// <summary> Gets a package by name, if it exists, but does not intern it </summary>
    public Package Find(string name)
    {
        for (int i = 0; i < _packages.Count; i++)
        {
            Entry pkg = _packages[i];
            if (pkg.Name == name)
            {
                return pkg.Package;
            }
        }
        return null;
    }

    /// <summary> Adds a new package, replacing an old one with the same name if it was already defined </summary>
    public Package Add(Package pkg)
    {
        int index = FindIndexOfPackage(pkg.Name);
        if (index >= 0) { _packages.RemoveAt(index); }
        _packages.Add(new Entry() { Name = pkg.Name, Package = pkg });
        return pkg;
    }

    /// <summary> Removes the package and returns true if successful. </summary>
    public bool Remove(Package pkg)
    {
        int index = FindIndexOfPackage(pkg.Name);
        if (index >= 0)
        {
            _packages.RemoveAt(index);
            return true;
        }
        else
        {
            return false;
        }
    }
}