using Paremnet.Data;
using Paremnet.Error;
using System.Collections.Generic;
using System.Linq;

namespace Paremnet.Core;

/// <summary>
/// Virtual machine state
/// </summary>
public sealed class State
{
    /// <summary> Closure containing current code block </summary>
    public Closure Fn = null;

    /// <summary> Program counter; index into the instruction list </summary>
    public int Pc = 0;

    /// <summary> Reference to the current environment (head of the chain of environments) </summary>
    public Environment Env = null;

    /// <summary> Stack of heterogeneous values (numbers, symbols, strings, closures, etc).
    /// Last item on the list is the top of the stack. </summary>
    public List<Val> Stack = new();

    /// <summary> Transient argument count register, used when calling functions </summary>
    public int Argcount = 0;

    /// <summary> Helper flag, stops the REPL </summary>
    public bool Done = false;

    public State(Closure closure, Val[] args)
    {
        Fn = closure;
        Env = Fn.Env;
        foreach (Val arg in args) { Stack.Add(arg); }
        Argcount = args.Length;
    }

    public void Push(Val v) => Stack.Add(v);

    public Val Pop()
    {
        int count = Stack.Count;
        if (count > 0)
        {
            Val result = Stack[count - 1];
            Stack.RemoveAt(count - 1);
            return result;
        }

        throw new LanguageError("Stack underflow!");
    }

    public Val Peek()
    {
        int count = Stack.Count;
        if (count > 0)
        {
            return Stack[count - 1];
        }

        throw new LanguageError("Stack underflow!");
    }

    internal static string PrintStack(State st) =>
        string.Format("{0,3}: [ {1} ]", st.Stack.Count,
            string.Join(" ", st.Stack.Select(val => Val.DebugPrint(val))));
}