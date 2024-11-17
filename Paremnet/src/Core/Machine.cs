using Paremnet.Data;
using Paremnet.Error;
using System.Collections.Generic;

namespace Paremnet.Core;

/// <summary>
/// Virtual machine that will interpret compiled bytecode
/// </summary>
public class Machine
{
    /// <summary> If set, instructions will be logged to this function as they're executed. </summary>
    private readonly ILogger _logger = null;

    /// <summary> Internal execution context </summary>
    private readonly Context _ctx = null;

    public Machine(Context ctx, ILogger logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    /// <summary> Runs the given piece of code, and returns the value left at the top of the stack. </summary>
    public Val Execute(Closure fn, params Val[] args)
    {
        State st = new(fn, args);
        CodeHandle code = default;
        List<Instruction> instructions = null;

        if (_logger.EnableInstructionLogging)
        {
            _logger.Log("Executing: ", fn.Name);
            _logger.Log(_ctx.Code.DebugPrint(fn));
        }

        while (!st.Done)
        {
            if (!code.Equals(st.Fn.Code))
            {
                code = st.Fn.Code;
                instructions = _ctx.Code.Get(code).Instructions;
            }

            if (st.Pc >= instructions.Count)
            {
                throw new LanguageError("Runaway opcodes!");
            }

            // fetch instruction
            Instruction instr = instructions[st.Pc++];

            if (_logger.EnableStackLogging)
            {
                _logger.Log("                                    " + State.PrintStack(st));
                _logger.Log(string.Format("[{0,2}] {1,3} : {2}", st.Stack.Count, st.Pc - 1, instr.DebugPrint()));
            }

            // and now a big old switch statement. not handler functions - this is much faster.

            switch (instr.Type)
            {
                case Opcode.Label:
                    // no op :)
                    break;

                case Opcode.PushConst:
                    {
                        st.Push(instr.First);
                    }
                    break;

                case Opcode.LocalGet:
                    {
                        VarPos pos = new(instr.First, instr.Second);
                        Val value = Environment.GetValueAt(pos, st.Env);
                        st.Push(value);
                    }
                    break;

                case Opcode.LocalSet:
                    {
                        VarPos pos = new(instr.First, instr.Second);
                        Val value = st.Peek();
                        Environment.SetValueAt(pos, value, st.Env);
                    }
                    break;

                case Opcode.GlobalGet:
                    {
                        Symbol symbol = instr.First.AsSymbol;
                        Val value = symbol.Pkg.GetValue(symbol);
                        st.Push(value);
                    }
                    break;

                case Opcode.GlobalSet:
                    {
                        Symbol symbol = instr.First.AsSymbol;
                        Val value = st.Peek();
                        symbol.Pkg.SetValue(symbol, value);
                    }
                    break;

                case Opcode.StackPop:
                    st.Pop();
                    break;

                case Opcode.JmpIfTrue:
                    {
                        Val value = st.Pop();
                        if (value.CastToBool)
                        {
                            st.Pc = GetLabelPosition(instr);
                        }
                    }
                    break;

                case Opcode.JmpIfFalse:
                    {
                        Val value = st.Pop();
                        if (!value.CastToBool)
                        {
                            st.Pc = GetLabelPosition(instr);
                        }
                    }
                    break;

                case Opcode.JmpToLabel:
                    {
                        st.Pc = GetLabelPosition(instr);
                    }
                    break;

                case Opcode.MakeEnv:
                    {
                        int argcount = instr.First.AsInt;
                        if (st.Argcount != argcount) { throw new LanguageError($"Argument count error, expected {argcount}, got {st.Argcount}"); }

                        // make an environment for the given number of named args
                        st.Env = new Environment(st.Argcount, st.Env);

                        // move named arguments onto the stack frame
                        for (int i = argcount - 1; i >= 0; i--)
                        {
                            st.Env.SetValue(i, st.Pop());
                        }
                    }
                    break;

                case Opcode.MakeEnvdot:
                    {
                        int argcount = instr.First.AsInt;
                        if (st.Argcount < argcount) { throw new LanguageError($"Argument count error, expected {argcount} or more, got {st.Argcount}"); }

                        // make an environment for all named args, +1 for the list of remaining varargs
                        int dotted = st.Argcount - argcount;
                        st.Env = new Environment(argcount + 1, st.Env);

                        // cons up dotted values from the stack
                        for (int dd = dotted - 1; dd >= 0; dd--)
                        {
                            Val arg = st.Pop();
                            st.Env.SetValue(argcount, new Val(new Cons(arg, st.Env.GetValue(argcount))));
                        }

                        // and move the named ones onto the environment stack frame
                        for (int i = argcount - 1; i >= 0; i--)
                        {
                            st.Env.SetValue(i, st.Pop());
                        }
                    }
                    break;

                case Opcode.Duplicate:
                    {
                        if (st.Stack.Count == 0) { throw new LanguageError("Cannot duplicate on an empty stack!"); }
                        st.Push(st.Peek());
                    }
                    break;

                case Opcode.JmpClosure:
                    {
                        st.Env = st.Env.Parent; // discard the top environment frame
                        Val top = st.Pop();
                        Closure closure = top.AsClosureOrNull;

                        // set vm state to the beginning of the closure
                        st.Fn = closure ?? throw new LanguageError($"Unknown function during function call around: {DebugRecentInstructions(st, instructions)}");
                        st.Env = closure.Env;
                        st.Pc = 0;
                        st.Argcount = instr.First.AsInt;
                    }
                    break;

                case Opcode.SaveReturn:
                    {
                        // save current vm state to a return value
                        st.Push(new Val(new ReturnAddress(st.Fn, GetLabelPosition(instr), st.Env, instr.First.AsStringOrNull)));
                    }
                    break;

                case Opcode.ReturnVal:
                    if (st.Stack.Count > 1)
                    {
                        // preserve return value on top of the stack
                        Val retval = st.Pop();
                        ReturnAddress retaddr = st.Pop().AsReturnAddress;
                        st.Push(retval);

                        // restore vm state from the return value
                        st.Fn = retaddr.Fn;
                        st.Env = retaddr.Env;
                        st.Pc = retaddr.Pc;
                    }
                    else
                    {
                        st.Done = true; // this will force the virtual machine to finish up
                    }
                    break;

                case Opcode.MakeClosure:
                    {
                        Closure cl = instr.First.AsClosure;
                        st.Push(new Closure(cl.Code, st.Env, null, cl.Name));
                    }
                    break;

                case Opcode.CallPrimop:
                    {
                        string name = instr.First.AsString;
                        int argn = (instr.Second.IsInt) ? instr.Second.AsInt : st.Argcount;

                        Primitive prim = Primitives.FindNary(name, argn);
                        if (prim == null) { throw new LanguageError($"Invalid argument count to primitive {name}, count of {argn}"); }

                        Val result = prim.Call(_ctx, argn, st);
                        st.Push(result);
                    }
                    break;

                case Opcode.Illegal:
                    throw new LanguageError("Encountered illegal instruction: " + instr.Type);

                case Opcode.NoOp:
                    break;

                default:
                    throw new LanguageError("Unknown instruction type: " + instr.Type);
            }
        }

        // return whatever's on the top of the stack
        if (st.Stack.Count == 0)
        {
            throw new LanguageError("Stack underflow!");
        }

        return st.Peek();
    }

    /// <summary> Very naive helper function, finds the position of a given label in the instruction set </summary>
    private static int GetLabelPosition(Instruction inst)
    {
        if (inst.Second.IsInt)
        {
            return inst.Second.AsInt;
        }
        else
        {
            throw new LanguageError("Unknown jump label: " + inst.First);
        }
    }

    /// <summary> A bit of debug info </summary>
    private static string DebugRecentInstructions(State st, List<Instruction> instructions)
    {
        string result = $"Closure {st.Fn.Code}, around instr pc {st.Pc - 1}:";
        for (int i = st.Pc - 5; i <= st.Pc; i++)
        {
            if (i >= 0 && i < instructions.Count)
            {
                result += $"{i}: {instructions[i].DebugPrint()}\n";
            }
        }
        return result;
    }
}
