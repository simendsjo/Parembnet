using Paremnet.Core;
using Paremnet.Error;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        Int64,
        UInt64,
        Int128,
        UInt128,

        Float32,
        Float64,
        Float128,

        // reference types
        Type,
        String,
        Symbol,
        Cons,
        Vector,
        Map,
        Closure,
        ReturnAddress,
        Object
    }

    // value types need to live at a separate offset from reference types
    [FieldOffset(0)] public readonly Int128 rawvalue;
    [FieldOffset(0)] public readonly Boolean vboolean;
    [FieldOffset(0)] public readonly sbyte vint8;
    [FieldOffset(0)] public readonly byte vuint8;
    [FieldOffset(0)] public readonly Int16 vint16;
    [FieldOffset(0)] public readonly UInt16 vuint16;
    [FieldOffset(0)] public readonly Int32 vint32;
    [FieldOffset(0)] public readonly UInt32 vuint32;
    [FieldOffset(0)] public readonly Int64 vint64;
    [FieldOffset(0)] public readonly UInt64 vuint64;
    [FieldOffset(0)] public readonly Int128 vint128;
    [FieldOffset(0)] public readonly UInt128 vuint128;
    [FieldOffset(0)] public readonly Single vfloat32;
    [FieldOffset(0)] public readonly double vfloat64;
    [FieldOffset(0)] public readonly decimal vfloat128;

    [FieldOffset(16)] public readonly object rawobject;
    [FieldOffset(16)] public readonly System.Type vtype;
    [FieldOffset(16)] public readonly string vstring;
    [FieldOffset(16)] public readonly Symbol vsymbol;
    [FieldOffset(16)] public readonly Cons vcons;
    [FieldOffset(16)] public readonly Vector vvector;
    [FieldOffset(16)] public readonly ImmutableDictionary<Val, Val> vmap;
    [FieldOffset(16)] public readonly Closure vclosure;
    [FieldOffset(16)] public readonly ReturnAddress vreturn;

    [FieldOffset(24)] public readonly Type type;

    public static readonly Val NIL = new(Type.Nil);

    public System.Type CliType =>
        type switch
        {
            Type.Boolean => typeof(System.Boolean),
            Type.Nil => typeof(Val),
            Type.Int8 => typeof(System.SByte),
            Type.UInt8 => typeof(System.Byte),
            Type.Int16 => typeof(System.Int16),
            Type.UInt16 => typeof(System.UInt16),
            Type.Int32 => typeof(System.Int32),
            Type.UInt32 => typeof(System.UInt32),
            Type.Int64 => typeof(System.Int64),
            Type.UInt64 => typeof(System.UInt64),
            Type.Int128 => typeof(System.Int128),
            Type.UInt128 => typeof(System.UInt128),
            Type.Float32 => typeof(System.Single),
            Type.Float64 => typeof(System.Double),
            Type.Float128 => typeof(System.Decimal),
            Type.Type => typeof(System.Type),
            Type.String => typeof(System.String),
            Type.Symbol => typeof(Symbol),
            Type.Cons => typeof(Cons),
            Type.Vector => typeof(Vector),
            Type.Map => typeof(ImmutableDictionary<Val, Val>),
            Type.Closure => typeof(Closure),
            Type.ReturnAddress => typeof(ReturnAddress),
            Type.Object => rawobject.GetType(),
            _ => throw new ArgumentOutOfRangeException()
        };

    private Val(Int128 rawvalue, object rawobject, Type type, ImmutableDictionary<Val, Val> metadata)
    {
        this.rawvalue = rawvalue;
        this.rawobject = rawobject;
        this.type = type;
        this.metadata = metadata;
    }

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

    public Val(Int64 value) : this()
    {
        type = Type.Int64;
        vint64 = value;
    }

    public Val(UInt64 value) : this()
    {
        type = Type.UInt64;
        vuint64 = value;
    }

    public Val(Int128 value) : this()
    {
        type = Type.Int128;
        vint128 = value;
    }

    public Val(UInt128 value) : this()
    {
        type = Type.UInt128;
        vuint128 = value;
    }

    public Val(Single value) : this()
    {
        type = Type.Float32;
        vfloat32 = value;
    }

    public Val(Double value) : this()
    {
        type = Type.Float64;
        vfloat64 = value;
    }

    public Val(Decimal value) : this()
    {
        type = Type.Float128;
        vfloat128 = value;
    }

    public Val(System.Type value) : this()
    {
        type = Type.Type;
        vtype = value;
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

    public Val(ImmutableDictionary<Val, Val> value) : this()
    {
        type = Type.Map;
        vmap = value;
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

    public static implicit operator Val(bool val) => new(val);

    public static implicit operator Val(sbyte val) => new(val);
    public static implicit operator Val(byte val) => new(val);
    public static implicit operator Val(Int16 val) => new(val);
    public static implicit operator Val(UInt16 val) => new(val);
    public static implicit operator Val(Int32 val) => new(val);
    public static implicit operator Val(UInt32 val) => new(val);
    public static implicit operator Val(Int64 val) => new(val);
    public static implicit operator Val(UInt64 val) => new(val);
    public static implicit operator Val(Int128 val) => new(val);
    public static implicit operator Val(UInt128 val) => new(val);

    public static implicit operator Val(Single val) => new(val);
    public static implicit operator Val(Double val) => new(val);
    public static implicit operator Val(Decimal val) => new(val);

    public static implicit operator Val(System.Type val) => new(val);
    public static implicit operator Val(string val) => new(val);
    public static implicit operator Val(Symbol val) => new(val);
    public static implicit operator Val(Cons val) => new(val);
    public static implicit operator Val(Vector val) => new(val);
    public static implicit operator Val(ImmutableDictionary<Val, Val> val) => new(val);
    public static implicit operator Val(Closure val) => new(val);

    public bool IsNil => type == Type.Nil;
    public bool IsNotNil => type != Type.Nil;
    public bool IsAtom => type != Type.Cons;

    public bool IsNumber => type is >= Type.Int8 and <= Type.Float64;

    public bool IsBool => type == Type.Boolean;

    public bool IsInt8 => type == Type.Int8;
    public bool IsUInt8 => type == Type.UInt8;
    public bool IsInt16 => type == Type.Int16;
    public bool IsUInt16 => type == Type.UInt16;
    public bool IsInt32 => type == Type.Int32;
    public bool IsUInt32 => type == Type.UInt32;
    public bool IsInt64 => type == Type.Int64;
    public bool IsUInt64 => type == Type.UInt64;
    public bool IsInt128 => type == Type.Int128;
    public bool IsUInt128 => type == Type.UInt128;

    public bool IsFloat32 => type == Type.Float32;
    public bool IsFloat64 => type == Type.Float64;
    public bool IsFloat128 => type == Type.Float128;

    public bool IsType => type == Type.Type;
    public bool IsString => type == Type.String;
    public bool IsSymbol => type == Type.Symbol;
    public bool IsCons => type == Type.Cons;
    public bool IsVector => type == Type.Vector;
    public bool IsMap => type == Type.Map;
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
    public Int64 AsInt64 => type == Type.Int64 ? vint64 : throw new CompilerError("Value type was expected to be Int64");
    public UInt64 AsUInt64 => type == Type.UInt64 ? vuint64 : throw new CompilerError("Value type was expected to be Int64");
    public Int128 AsInt128 => type == Type.Int128 ? vint128 : throw new CompilerError("Value type was expected to be Int128");
    public UInt128 AsUInt128 => type == Type.UInt128 ? vuint128 : throw new CompilerError("Value type was expected to be Int128");

    public Single AsFloat32 => type == Type.Float32 ? vfloat32 : throw new CompilerError("Value type was expected to be Float32");
    public Double AsFloat64 => type == Type.Float64 ? vfloat64 : throw new CompilerError("Value type was expected to be Float64");
    public Decimal AsFloat128 => type == Type.Float128 ? vfloat128 : throw new CompilerError("Value type was expected to be Float128");

    public System.Type AsType => type == Type.Type ? vtype : throw new CompilerError("Value type was expected to be Type");
    public string AsString => type == Type.String ? vstring : throw new CompilerError("Value type was expected to be string");
    public Symbol AsSymbol => type == Type.Symbol ? vsymbol : throw new CompilerError("Value type was expected to be symbol");
    public Cons AsCons => type == Type.Cons ? vcons : throw new CompilerError("Value type was expected to be cons");
    public Vector AsVector => type == Type.Vector ? vvector : throw new CompilerError("Value type was expected to be vector");
    public ImmutableDictionary<Val, Val> AsMap => type == Type.Map ? vmap : throw new CompilerError("Value type was expected to be Map");
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
            Type.UInt32 => vuint32,
            Type.Int64 => vint64,
            Type.UInt64 => vuint64,
            Type.Int128 => vint128,
            Type.UInt128 => vuint128,
            Type.Float32 => vfloat32,
            Type.Float64 => vfloat64,
            Type.Float128 => vfloat128,
            Type.Type => vtype,
            Type.String => vstring,
            Type.Symbol => vsymbol,
            Type.Cons => vcons,
            Type.Vector => vvector,
            Type.Map => vmap,
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
            UInt32 uint32 => uint32,
            UInt64 uint64 => uint64,
            UInt128 uint128 => uint128,
            Single float32 => float32,
            Double float64 => float64,
            Decimal float128 => float128,
            System.Type type => type,
            string str => str,
            Symbol symbol => symbol,
            Cons cons => cons,
            Vector vec => vec,
            ImmutableDictionary<Val, Val> map => map,
            Closure closure => closure,
            _ => new Val(boxed),
        };

    public bool CastToBool => (type == Type.Boolean) ? vboolean : (type != Type.Nil);

    public Single CastToFloat32 =>
        type switch
        {
            Type.Int8 => vint8,
            Type.UInt8 => vuint8,
            Type.Int16 => vint16,
            Type.UInt16 => vuint16,
            Type.Int32 => vint32,
            Type.UInt32 => vuint32,
            Type.Float32 => vfloat32,
            _ => throw new CompilerError($"Cannot cast {type} to Float32")
        };

    private bool IsValueType => type is >= Type.Boolean and <= Type.Float128;

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

        // value type equality for maps
        if (a.type == Type.Map)
        {
            if (a.vmap.Count != b.vmap.Count)
            {
                return false;
            }

            var ae = a.vmap.GetEnumerator();
            var be = b.vmap.GetEnumerator();
            while(true)
            {
                var an = ae.MoveNext();
                var bn = be.MoveNext();

                // One empty, but not the other
                if (an != bn)
                {
                    return false;
                }

                // Both empty
                if (an == false)
                {
                    return true;
                }

                if (!Equals(ae.Current, be.Current))
                {
                    return false;
                }
            }
        }

        // otherwise if it's a reference type, compare object reference
        return ReferenceEquals(a.rawobject, b.rawobject);
    }

    public bool Equals(Val other) => Equals(this, other);

    public static bool operator ==(Val a, Val b) => Equals(a, b);
    public static bool operator !=(Val a, Val b) => !Equals(a, b);

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
            case Type.UInt32:
                return val.vuint32.ToString(CultureInfo.InvariantCulture);
            case Type.Int64:
                return val.vint64.ToString(CultureInfo.InvariantCulture);
            case Type.UInt64:
                return val.vuint64.ToString(CultureInfo.InvariantCulture);
            case Type.Int128:
                return val.vint128.ToString(CultureInfo.InvariantCulture);
            case Type.UInt128:
                return val.vuint128.ToString(CultureInfo.InvariantCulture);
            case Type.Float32:
                return val.vfloat32.ToString(CultureInfo.InvariantCulture);
            case Type.Float64:
                return val.vfloat64.ToString(CultureInfo.InvariantCulture);
            case Type.Float128:
                return val.vfloat128.ToString(CultureInfo.InvariantCulture);
            case Type.Type:
                return val.vtype.ToString();
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
            case Type.Map:
                {
                    StringBuilder sb = new();
                    sb.Append('{');
                    var e = val.vmap.GetEnumerator();
                    if (e.MoveNext())
                    {
                        KeyValuePair<Val, Val> kv = e.Current;
                        sb.Append(Print(kv.Key));
                        sb.Append(' ');
                        sb.Append(Print(kv.Value));

                        while(e.MoveNext())
                        {
                            kv = e.Current;
                            sb.Append(' ');
                            sb.Append(Print(kv.Key));
                            sb.Append(' ');
                            sb.Append(Print(kv.Value));
                        }
                    }
                    sb.Append('}');
                    return sb.ToString();
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
