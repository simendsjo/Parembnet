using Paremnet.Data;
using Paremnet.Error;
using System.Collections.Generic;

namespace Paremnet.Core;

/// <summary>
/// Special wrapper around cons for primitive invocations,
/// so that we avoid ambiguiting with passing in Vals
/// </summary>
public struct VarArgs(Cons cons)
{
    public Cons Cons = cons;

    public bool IsCons => Cons != null;
    public bool IsNull => Cons == null;

    public Val AsVal => Cons ?? Val.NIL;

    public List<Val> ToNativeList() => Cons.ToNativeList(AsVal);

    public static implicit operator VarArgs(Cons c) => new(c);
}

public delegate Val FnThunk(Context ctx);
public delegate Val FnUnary(Context ctx, Val a);
public delegate Val FnBinary(Context ctx, Val a, Val b);
public delegate Val FnTernary(Context ctx, Val a, Val b, Val c);
public delegate Val FnVarArg(Context ctx, VarArgs args);

/// <summary>
/// Holds a reference to a primitive function.
/// This wrapper is intended to simplify function calls of various arities
/// without going through reflection, and incurring the perf cost.
/// </summary>
public class Function
{
    public FnThunk FnThunk;
    public FnUnary FnUnary;
    public FnBinary FnBinary;
    public FnTernary FnTernary;
    public FnVarArg FnVarArg;

    public Function(FnThunk fn) { FnThunk = fn; }
    public Function(FnUnary fn) { FnUnary = fn; }
    public Function(FnBinary fn) { FnBinary = fn; }
    public Function(FnTernary fn) { FnTernary = fn; }
    public Function(FnVarArg fn) { FnVarArg = fn; }

    public Val Call(Context ctx)
    {
        if (FnThunk != null)
        {
            return FnThunk(ctx);
        }
        else if (FnVarArg != null)
        {
            return FnVarArg(ctx, null);
        }
        else
        {
            throw new LanguageError("Primitive function call of incorrect zero arity");
        }
    }

    public Val Call(Context ctx, Val a)
    {
        if (FnUnary != null)
        {
            return FnUnary(ctx, a);
        }
        else if (FnVarArg != null)
        {
            return FnVarArg(ctx, Cons.MakeList(a));
        }
        else
        {
            throw new LanguageError("Primitive function call of incorrect unary arity");
        }
    }

    public Val Call(Context ctx, Val a, Val b)
    {
        if (FnBinary != null)
        {
            return FnBinary(ctx, a, b);
        }
        else if (FnVarArg != null)
        {
            return FnVarArg(ctx, Cons.MakeList(a, b));
        }
        else
        {
            throw new LanguageError("Primitive function call of incorrect binary arity");
        }
    }

    public Val Call(Context ctx, Val a, Val b, Val c)
    {
        if (FnTernary != null)
        {
            return FnTernary(ctx, a, b, c);
        }
        else if (FnVarArg != null)
        {
            return FnVarArg(ctx, Cons.MakeList(a, b, c));
        }
        else
        {
            throw new LanguageError("Primitive function call of incorrect binary arity");
        }
    }

    public Val Call(Context ctx, Cons args)
    {
        if (FnVarArg != null)
        {
            return FnVarArg(ctx, args);
        }
        else
        {
            throw new LanguageError("Primitive function call of incorrect variable arity");
        }
    }
}

/// <summary>
/// Describes whether a primitive function has constant or variable number of arguments
/// </summary>
public enum FnType { ConstArgs, VarArgs };

/// <summary>
/// Describes whether a primitive function may cause side effects, or whether it's a pure function.
/// Pure functions may be optimized away if their outputs are never consumed.
/// </summary>
public enum SideFx { None, Possible }

/// <summary>
/// Built-in primitive functions, which all live in the core package.
/// </summary>
public class Primitive(
    string name,
    int minargs,
    Function fn,
    FnType argsType = FnType.ConstArgs,
    SideFx sideFx = SideFx.None)
{
    public readonly string Name = name;
    public readonly int Minargs = minargs;
    public readonly Function Fn = fn;
    public readonly FnType ArgsType = argsType; // is this a function with exact or variable number of arguments?
    public readonly SideFx SideFx = sideFx;   // does this primitive cause side effects? if so, it should never be optimized away

    public bool IsExact => ArgsType == FnType.ConstArgs;
    public bool IsVarArg => ArgsType == FnType.VarArgs;

    public bool HasSideEffects => SideFx == SideFx.Possible;
    public bool IsPureFunction => SideFx == SideFx.None;

    /// <summary> Calls the primitive function with argn operands waiting for it on the stack </summary>
    public Val Call(Context ctx, int argn, State state)
    {
        switch (argn)
        {
            case 0:
                {
                    return Fn.Call(ctx);
                }
            case 1:
                {
                    Val first = state.Pop();
                    return Fn.Call(ctx, first);
                }
            case 2:
                {
                    Val second = state.Pop();
                    Val first = state.Pop();
                    return Fn.Call(ctx, first, second);
                }
            case 3:
                {
                    Val third = state.Pop();
                    Val second = state.Pop();
                    Val first = state.Pop();
                    return Fn.Call(ctx, first, second, third);
                }
            default:
                {
                    Cons args = RemoveArgsFromStack(state, argn);
                    return Fn.Call(ctx, args);
                }
        }
    }

    private static Cons RemoveArgsFromStack(State state, int count)
    {
        Val result = Val.NIL;
        for (int i = 0; i < count; i++)
        {
            result = new Cons(state.Pop(), result);
        }
        return result.AsConsOrNull;
    }
}