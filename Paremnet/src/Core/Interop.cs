﻿using Paremnet.Data;
using Paremnet.Error;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Paremnet.Core;

/// <summary>
/// This static class manages .NET interop and looking up types, methods, etc. via reflection,
/// and then calling methods or getting / setting values.
/// </summary>
public static class Interop
{
    public const char NS_SEPARATOR = '.';

    /// <summary>
    /// This object wraps the name of a namespace, and it's used
    /// to dereference a type by type name and namespace name.
    /// </summary>
    [DebuggerDisplay("{DebugString}")]
    public class Namespace
    {
        public string ns;

        public (Type type, Namespace ns) FindTypeOrNamespace(string name)
        {
            string combined = ns + NS_SEPARATOR + name;
            Type type = TypeUtils.GetType(combined);
            Namespace newns = type == null ? new Namespace { ns = combined } : null;
            return (type, newns);
        }

        private string DebugString => ToString();
        public override string ToString() => $"[ns {ns}]";
    }

    //
    //
    // dotnet interop

    public static Val DotDot(Context _, VarArgs args)
    {

        List<Val> arglist = args.ToNativeList().SelectMany(SplitSymbol).ToList();

        bool gotone = arglist.Count > 0;
        if (!gotone) { return Val.NIL; }

        int i = 0;
        Val current = arglist[i++];

        do
        {
            Symbol nextSymbol = TakeNextSymbolOrNull();
            List<Val> nonSymbols = TakeNonSymbols().ToList();
            current = TryReflectionStep(current, nextSymbol, nonSymbols);
        } while (i < arglist.Count);

        return current;

        Symbol TakeNextSymbolOrNull()
        {
            IEnumerable<Val> results = TakeSymbols(true).Take(1);
            return results.FirstOrDefault().AsSymbolOrNull;
        }

        IEnumerable<Val> TakeNonSymbols()
        {
            return TakeSymbols(false);
        }

        IEnumerable<Val> TakeSymbols(bool expected)
        {
            while (i < arglist.Count)
            {
                Val next = arglist[i];
                if (next.IsSymbol == expected)
                {
                    i++;
                    yield return next;
                }
                else
                {
                    break;
                }
            }
        }
    }

    public static List<Val> SplitSymbol(Val val)
    {

        if (!val.IsSymbol) { return new List<Val>() { val }; }

        Symbol sym = val.AsSymbol;
        string symname = sym.name;
        if (!symname.Contains(NS_SEPARATOR)) { return new List<Val>() { val }; }

        // split on the dot
        string[] names = symname.Split(NS_SEPARATOR);
        return names.Select(n => new Val(sym.pkg.Intern(n))).ToList();
    }


    /// <summary>
    /// Indirect reflection step. In the case of a dotdot call like
    /// '(.. "foobar" 'Length 'ToString "D")
    /// the first call should evaluate to "foobar".Length
    /// and then a subsequent call should evaluate current.ToSTring("D")
    /// </summary>
    private static Val TryReflectionStep(Val current, Symbol nextSymbol, List<Val> nonSymbols)
    {

        if (current.IsBool || current.IsNumber || current.IsString)
        {
            return TryLookupOnInstance(current.AsBoxedValue, nextSymbol, nonSymbols);
        }

        if (current.IsSymbol)
        { // this should only happen on the very first element?!
            Namespace result = new Namespace() { ns = current.AsSymbol.name };
            return TryNamespaceLookup(result, nextSymbol, nonSymbols);
        }

        object obj = current.AsObjectOrNull;
        return obj switch
        {
            Namespace ns => TryNamespaceLookup(ns, nextSymbol, nonSymbols),
            Type type => TryLookupOnType(type, nextSymbol, nonSymbols),
            object other => TryLookupOnInstance(other, nextSymbol, nonSymbols),
            _ => throw new InteropError($"Not sure what to do with current {current}"),
        };
    }

    /// <summary>
    /// Looks up a specific namespace, either in specific parent namespace, or in root
    /// (the latter is only meaningful if it's the first symbol in the sequence)
    /// </summary>
    private static Val TryNamespaceLookup(Namespace ns, Symbol name, List<Val> nonSymbols)
    {
        if (nonSymbols.Count > 0) { throw new InteropError($"Unexpected non-symbols following {ns} {name}"); }
        (Type type, Namespace ns) results = ns.FindTypeOrNamespace(name.name);
        return new Val((object)results.type ?? results.ns);
    }

    /// <summary>
    /// Looks up named symbol on the type, suggesting it's a static field
    /// </summary>
    private static Val TryLookupOnType(Type type, Symbol name, List<Val> nonSymbols)
    {

        // is this a static field or property? look up its value
        MemberInfo fieldOrProp = TypeUtils.GetFieldOrProp(type, name.name, false);
        if (fieldOrProp != null)
        {
            if (nonSymbols.Count > 0) { throw new InteropError($"Unexpected non-symbols following {type} {name}"); }
            object result = LookupStaticFieldOrProp(fieldOrProp);
            return Val.TryUnbox(result);
        }

        // is this a static function? see if we can call it with the args
        object[] args = nonSymbols.Select(v => v.AsBoxedValue).ToArray();
        MethodBase fn = TypeUtils.GetMethodByArgs(type, name.name, false, args);
        if (fn != null)
        {
            object result = fn.Invoke(null, BindingFlags.Static, null, args, null);
            return Val.TryUnbox(result);
        }

        throw new InteropError($"Did not find static member corresponding to {type.Name}.{name.name} with correct arity and argument types");
    }


    private static object LookupStaticFieldOrProp(MemberInfo fieldOrProp, object[] args = null) =>
        fieldOrProp switch
        {
            FieldInfo fi => fi.GetValue(null),
            PropertyInfo pi => pi.GetValue(null, BindingFlags.Static, null, args, null),
            _ => throw new InteropError($"Unknown type of {fieldOrProp}"),
        };

    private static object LookupInstanceFieldOrProp(MemberInfo fieldOrProp, object instance, object[] args = null) =>
        fieldOrProp switch
        {
            FieldInfo fi => fi.GetValue(instance),
            PropertyInfo pi => pi.GetValue(instance, BindingFlags.Instance, null, args, null),
            _ => throw new InteropError($"Unknown type of {fieldOrProp}"),
        };

    /// <summary>
    /// Finds an instance member, either the field/property, or a method name
    /// </summary>
    private static Val TryLookupOnInstance(object instance, Symbol name, List<Val> nonSymbols)
    {
        if (instance == null) { throw new InteropError($"Unexpected null value before {name}"); }

        Type type = instance.GetType();
        object[] args = nonSymbols.Select(v => v.AsBoxedValue).ToArray();

        // is this an instance field or property? look up its value
        MemberInfo fieldOrProp = TypeUtils.GetFieldOrProp(type, name.name, true);
        if (fieldOrProp != null)
        {
            object result = LookupInstanceFieldOrProp(fieldOrProp, instance, args);
            return Val.TryUnbox(result);
        }

        // is this an instance function? see if we can call it with the args
        MethodBase fn = TypeUtils.GetMethodByArgs(type, name.name, true, args);
        if (fn != null)
        {
            object result = fn.Invoke(instance, BindingFlags.Instance, null, args, null);
            return Val.TryUnbox(result);
        }

        throw new InteropError($"Did not find instance member corresponding to {type.Name}.{name.name} with correct arity and argument types");
    }
}
