﻿using Paremnet.Core;
using Paremnet.Error;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

// ReSharper disable BuiltInTypeReferenceStyle
// We want to use explicit type names as they map directly into the CLI rather than using C# specific names.

namespace Paremnet.Data;

/// <summary>
/// Tagged struct that holds a variety of results:
/// strings, symbols, numbers, bools, closures, and others.
///
/// By using a tagged struct we avoid the need to box value types.
/// </summary>
[DebuggerDisplay("{DebugString}")]
[StructLayout(LayoutKind.Explicit)]
public readonly struct Val : IEquatable<Val>
{
    public enum Type : int
    {
        // value types
        Nil,

        Boolean,

        Int8,
        UInt8,
        Int16,
        UInt16,
        Int32,
        UInt32,

        Float32,
        Float64,

        // reference types
        String,
        Symbol,
        Cons,
        Vector,
        Closure,
        ReturnAddress,
        Object
    }

    // value types need to live at a separate offset from reference types
    [FieldOffset(0)] public readonly ulong rawvalue;
    [FieldOffset(0)] public readonly Boolean vboolean;
    [FieldOffset(0)] public readonly sbyte vint8;
    [FieldOffset(0)] public readonly byte vuint8;
    [FieldOffset(0)] public readonly Int16 vint16;
    [FieldOffset(0)] public readonly UInt16 vuint16;
    [FieldOffset(0)] public readonly Int32 vint32;
    [FieldOffset(0)] public readonly UInt32 vuint32;
    [FieldOffset(0)] public readonly Single vfloat32;
    [FieldOffset(0)] public readonly double vfloat64;

    [FieldOffset(8)] public readonly object rawobject;
    [FieldOffset(8)] public readonly string vstring;
    [FieldOffset(8)] public readonly Symbol vsymbol;
    [FieldOffset(8)] public readonly Cons vcons;
    [FieldOffset(8)] public readonly Vector vvector;
    [FieldOffset(8)] public readonly Closure vclosure;
    [FieldOffset(8)] public readonly ReturnAddress vreturn;

    [FieldOffset(16)] public readonly Type type;

    public static readonly Val NIL = new(Type.Nil);

    private Val(Type type) : this()
    {
        this.type = type;
    }

    public Val(Boolean value) : this()
    {
        type = Type.Boolean;
        vboolean = value;
    }

    public Val(sbyte value) : this()
    {
        type = Type.Int8;
        vint8 = value;
    }

    public Val(byte value) : this()
    {
        type = Type.UInt8;
        vuint8 = value;
    }

    public Val(Int16 value) : this()
    {
        type = Type.Int16;
        vint16 = value;
    }

    public Val(UInt16 value) : this()
    {
        type = Type.UInt16;
        vuint16 = value;
    }

    public Val(Int32 value) : this()
    {
        type = Type.Int32;
        vint32 = value;
    }

    public Val(UInt32 value) : this()
    {
        type = Type.UInt32;
        vuint32 = value;
    }

    public Val(Double value) : this()
    {
        type = Type.Float64;
        vfloat64 = value;
    }

    public Val(Single value) : this()
    {
        type = Type.Float32;
        vfloat32 = value;
    }

    public Val(string value) : this()
    {
        type = Type.String;
        vstring = value;
    }

    public Val(Symbol value) : this()
    {
        type = Type.Symbol;
        vsymbol = value;
    }

    public Val(Cons value) : this()
    {
        type = Type.Cons;
        vcons = value;
    }

    public Val(Vector value) : this()
    {
        type = Type.Vector;
        vvector = value;
    }

    public Val(Closure value) : this()
    {
        type = Type.Closure;
        vclosure = value;
    }

    public Val(ReturnAddress value) : this()
    {
        type = Type.ReturnAddress;
        vreturn = value;
    }

    public Val(object value) : this()
    {
        type = Type.Object;
        rawobject = value;
    }

    public bool IsNil => type == Type.Nil;
    public bool IsNotNil => type != Type.Nil;
    public bool IsAtom => type != Type.Cons;

    public bool IsNumber => type is >= Type.Int8 and <= Type.Float64;

    public bool IsBool => type == Type.Boolean;
    public bool IsUInt8 => type == Type.UInt8;
    public bool IsInt8 => type == Type.Int8;
    public bool IsUInt16 => type == Type.UInt16;
    public bool IsInt16 => type == Type.Int16;
    public bool IsInt32 => type == Type.Int32;
    public bool IsUInt32 => type == Type.UInt32;
    public bool IsFloat32 => type == Type.Float32;
    public bool IsFloat64 => type == Type.Float64;
    public bool IsString => type == Type.String;
    public bool IsSymbol => type == Type.Symbol;
    public bool IsCons => type == Type.Cons;
    public bool IsVector => type == Type.Vector;
    public bool IsClosure => type == Type.Closure;
    public bool IsReturnAddress => type == Type.ReturnAddress;
    public bool IsObject => type == Type.Object;

    public Boolean AsBoolean => type == Type.Boolean ? vboolean : throw new CompilerError("Value type was expected to be Boolean");
    public sbyte AsInt8 => type == Type.Int8 ? vint8 : throw new CompilerError("Value type was expected to be Int8");
    public byte AsUInt8 => type == Type.UInt8 ? vuint8 : throw new CompilerError("Value type was expected to be Int8");
    public Int16 AsInt16 => type == Type.Int16 ? vint16 : throw new CompilerError("Value type was expected to be Int16");
    public UInt16 AsUInt16 => type == Type.UInt16 ? vuint16 : throw new CompilerError("Value type was expected to be Int16");
    public Int32 AsInt32 => type == Type.Int32 ? vint32 : throw new CompilerError("Value type was expected to be Int32");
    public UInt32 AsUInt32 => type == Type.UInt32 ? vuint32 : throw new CompilerError("Value type was expected to be Int32");
    public string AsString => type == Type.String ? vstring : throw new CompilerError("Value type was expected to be string");
    public Symbol AsSymbol => type == Type.Symbol ? vsymbol : throw new CompilerError("Value type was expected to be symbol");
    public Cons AsCons => type == Type.Cons ? vcons : throw new CompilerError("Value type was expected to be cons");
    public Vector AsVector => type == Type.Vector ? vvector : throw new CompilerError("Value type was expected to be vector");
    public Closure AsClosure => type == Type.Closure ? vclosure : throw new CompilerError("Value type was expected to be closure");
    public ReturnAddress AsReturnAddress => type == Type.ReturnAddress ? vreturn : throw new CompilerError("Value type was expected to be ret addr");
    public object AsObject => type == Type.Object ? rawobject : throw new CompilerError("Value type was expected to be object");

    public string AsStringOrNull => type == Type.String ? vstring : null;
    public Symbol AsSymbolOrNull => type == Type.Symbol ? vsymbol : null;
    public Cons AsConsOrNull => type == Type.Cons ? vcons : null;
    public Vector AsVectorOrNull => type == Type.Vector ? vvector : null;
    public Closure AsClosureOrNull => type == Type.Closure ? vclosure : null;
    public object AsObjectOrNull => type == Type.Object ? rawobject : null;

    public T GetObjectOrNull<T>() where T : class =>
        type == Type.Object && rawobject is T obj ? obj : null;

    public object AsBoxedValue =>
        type switch
        {
            Type.Nil => null,
            Type.Boolean => vboolean,
            Type.Int8 => vint8,
            Type.UInt8 => vuint8,
            Type.Int16 => vint16,
            Type.UInt16 => vuint16,
            Type.Int32 => vint32,
            Type.Float32 => vfloat32,
            Type.Float64 => vfloat64,
            Type.String => vstring,
            Type.Symbol => vsymbol,
            Type.Cons => vcons,
            Type.Vector => vvector,
            Type.Closure => vclosure,
            Type.ReturnAddress => vreturn,
            Type.Object => rawobject,
            _ => throw new LanguageError("Unexpected value type: " + type),
        };

    public static Val TryUnbox(object boxed) =>
        boxed switch
        {
            null => NIL,
            Boolean boolean => boolean,
            sbyte int8 => int8,
            byte uint8 => uint8,
            Int16 int16 => int16,
            UInt16 uint16 => uint16,
            Int32 int32 => int32,
            Single float32 => float32,
            Double float64 => float64,
            string str => str,
            Symbol symbol => symbol,
            Cons cons => cons,
            Vector vec => vec,
            Closure closure => closure,
            _ => new Val(boxed),
        };

    public bool CastToBool => (type == Type.Boolean) ? vboolean : (type != Type.Nil);

    public float CastToSingle =>
        type switch
        {
            Type.Int16 => vint16,
            Type.UInt16 => vuint16,
            Type.Int32 => vint32,
            Type.Float32 => vfloat32,
            Type.Float64 => throw new CompilerError("Cannot cast Float64 to Float32"),
            _ => throw new CompilerError("Float32 cast applied to not a number")
        };

    private bool IsValueType => type is >= Type.Boolean and <= Type.Float64;

    public static bool Equals(Val a, Val b)
    {
        if (a.type != b.type) { return false; }

        // same type, if it's a string we need to do a string equals
        if (a.type == Type.String)
        {
            return string.Equals(a.vstring, b.vstring, StringComparison.InvariantCulture);
        }

        // if it's a value type, simply compare the value data
        if (a.IsValueType) { return a.rawvalue == b.rawvalue; }

        // otherwise if it's a reference type, compare object reference
        return ReferenceEquals(a.rawobject, b.rawobject);
    }

    public bool Equals(Val other) => Equals(this, other);

    public static bool operator ==(Val a, Val b) => Equals(a, b);
    public static bool operator !=(Val a, Val b) => !Equals(a, b);

    public static implicit operator Val(bool val) => new(val);
    public static implicit operator Val(sbyte val) => new(val);
    public static implicit operator Val(byte val) => new(val);
    public static implicit operator Val(Int16 val) => new(val);
    public static implicit operator Val(UInt16 val) => new(val);
    public static implicit operator Val(Int32 val) => new(val);
    public static implicit operator Val(Single val) => new(val);
    public static implicit operator Val(Double val) => new(val);
    public static implicit operator Val(string val) => new(val);
    public static implicit operator Val(Symbol val) => new(val);
    public static implicit operator Val(Cons val) => new(val);
    public static implicit operator Val(Vector val) => new(val);
    public static implicit operator Val(Closure val) => new(val);

    public override bool Equals(object obj) => (obj is Val val) && Equals(val, this);
    public override int GetHashCode() => (int)type ^ (rawobject != null ? rawobject.GetHashCode() : ((int)rawvalue));

    private string DebugString => $"{Print(this, false)} [{type}]";
    public override string ToString() => Print(this, true);

    public static string DebugPrint(Val val) => Print(val, false);
    public static string Print(Val val) => Print(val, true);

    private static string Print(Val val, bool fullName)
    {
        switch (val.type)
        {
            case Type.Nil:
                return "()";
            case Type.Boolean:
                return val.vboolean ? "#t" : "#f";
            case Type.Int8:
                return val.vint8.ToString(CultureInfo.InvariantCulture);
            case Type.UInt8:
                return val.vuint8.ToString(CultureInfo.InvariantCulture);
            case Type.Int16:
                return val.vint16.ToString(CultureInfo.InvariantCulture);
            case Type.UInt16:
                return val.vuint16.ToString(CultureInfo.InvariantCulture);
            case Type.Int32:
                return val.vint32.ToString(CultureInfo.InvariantCulture);
            case Type.Float32:
                return val.vfloat32.ToString(CultureInfo.InvariantCulture);
            case Type.Float64:
                return val.vfloat64.ToString(CultureInfo.InvariantCulture);
            case Type.String:
                return "\"" + val.vstring + "\"";
            case Type.Symbol:
                return fullName ? val.vsymbol.FullName : val.vsymbol.Name;
            case Type.Cons:
                return StringifyCons(val.vcons, fullName);
            case Type.Vector:
                {
                    string elements = val.vvector.Print(" ");
                    return $"[Vector {elements}]";
                }
            case Type.Closure:
                return string.IsNullOrEmpty(val.vclosure.Name) ? "[Closure]" : $"[Closure/{val.vclosure.Name}]";
            case Type.ReturnAddress:
                return $"[{val.vreturn.Debug}/{val.vreturn.Pc}]";
            case Type.Object:
                {
                    string typedesc = val.rawobject == null ? "null" : $"{val.rawobject.GetType()} {val.rawobject}";
                    return $"[Native {typedesc}]";
                }
            default:
                throw new CompilerError("Unexpected value type: " + val.type);
        }
    }

    /// <summary> Helper function for cons cells </summary>
    private static string StringifyCons(Cons cell, bool fullName)
    {
        StringBuilder sb = new();
        sb.Append('(');

        Val val = new(cell);
        while (val.IsNotNil)
        {
            Cons cons = val.AsConsOrNull;
            if (cons != null)
            {
                sb.Append(Print(cons.First, fullName));
                if (cons.Rest.IsNotNil)
                {
                    sb.Append(' ');
                }
                val = cons.Rest;
            }
            else
            {
                sb.Append(". ");
                sb.Append(Print(val, fullName));
                val = NIL;
            }
        }

        sb.Append(')');
        return sb.ToString();
    }
}
