using Paremnet.Error;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Paremnet.Data;

/// <summary> Enum of instructions produced by the compiler </summary>
public enum Opcode
{
    /// <summary>
    /// Just a label, doesn't do anything, only used during compilation
    /// </summary>
    Label,

    /// <summary>
    /// PUSH_CONST x - pushes x onto the stack
    /// </summary>
    PushConst,

    /// <summary>
    /// LOCAL_GET i j -  push local variable onto the stack, where <b>i</b> is the frame index relative
    ///                  to current frame and <b>j</b> is the symbol index
    /// </summary>
    LocalGet,

    /// <summary>
    /// LOCAL_SET i, j - set local variable from what's on top of the stack, without popping from the stack,
    ///                  where <b>i</b> is the frame index relative to current frame and <b>j</b> is the symbol index
    /// </summary>
    LocalSet,

    /// <summary>
    /// GLOBAL_GET name - push global variable onto the stack
    /// </summary>
    GlobalGet,

    /// <summary>
    /// GLOBAL_SET name - set global variable from what's on top of the stack, without popping the stack
    /// </summary>
    GlobalSet,

    /// <summary>
    /// STACK_POP - pops the top value from the stack, discarding it
    /// </summary>
    StackPop,

    /// <summary>
    /// DUPLICATE - duplicates (pushes a second copy of) the topmost value on the stack
    /// </summary>
    Duplicate,

    /// <summary>
    /// JMP_IF_TRUE label - pop the stack, and jump to label if the value is true
    /// </summary>
    JmpIfTrue,

    /// <summary>
    /// JMP_IF_FALSE label - pop the stack, and jump to label if the value is not true
    /// </summary>
    JmpIfFalse,

    /// <summary>
    /// JMP_TO_LABEL label - jump to label without modifying or looking up the stack
    /// </summary>
    JmpToLabel,

    /// <summary>
    /// SAVE - save continuation point on the stack, as a combo of specific function, program counter,
    ///        and environment
    /// </summary>
    SaveReturn,

    /// <summary>
    /// JMP_CLOSURE n - jump to the start of the function on top of the stack; n is arg count
    /// </summary>
    JmpClosure,

    /// <summary>
    /// RETURN - return to a previous execution point (second on the stack) but preserving
    ///          the return value (top of the stack)
    /// </summary>
    ReturnVal,

    /// <summary>
    /// MAKE_ENV n - make a new environment frame, pop n values from stack onto it,
    ///              and push it on the environment stack
    /// </summary>
    MakeEnv,

    /// <summary>
    /// MAKE_ENVDOT n - make a new environment frame with n-1 named args and one for varargs,
    ///                 pop values from stack onto it, and push on the environment stack
    /// </summary>
    MakeEnvdot,

    /// <summary>
    /// MAKE_CLOSURE fn - create a closure fn from arguments and current environment, and push onto the stack
    /// </summary>
    MakeClosure,

    /// <summary>
    /// CALL_PRIMOP name - performs a primitive function call right off of the stack, where callee performs
    ///             stack maintenance (i.e. the primitive will pop its args, and push a return value)
    /// </summary>
    CallPrimop,
}

/// <summary>
/// Instructions produced by the compiler
/// </summary>
[DebuggerDisplay("{DebugString}")]
public class Instruction(Opcode type, Val first, Val second, string debug = null)
{
    /// <summary> ArrayList of human readable names for all constants </summary>
    private static readonly string[] Names = Enum.GetNames(typeof(Opcode));

    /// <summary> Names of all jump instructions that need to be fixed up at assembly time </summary>
    private static readonly List<Opcode> JumpTypes =
    [
        Opcode.JmpToLabel,
        Opcode.JmpIfFalse,
        Opcode.JmpIfTrue,
        Opcode.SaveReturn
    ];

    /// <summary> Instruction type, one of the constants in this class </summary>
    public Opcode Type { get; } = type;

    /// <summary> First instruction parameter (context-sensitive) </summary>
    public Val First { get; } = first;

    /// <summary> Second instruction parameter (context-sensitive) </summary>
    public Val Second { get; private set; } = second;

    /// <summary> Debug information (printed to the user as needed) </summary>
    public readonly string Debug = debug;

    public Instruction(Opcode type) : this(type, Val.NIL, Val.NIL, null) { }
    public Instruction(Opcode type, Val first, string debug = null) : this(type, first, Val.NIL, debug) { }

    /// <summary> Is this instruction one of the jump instructions that needs to be modified during assembly? </summary>
    public bool IsJump => JumpTypes.Contains(Type);

    /// <summary> If this is a jump instruction, updates the second parameter to contain the destination </summary>
    public void UpdateJumpDestination(int pc)
    {
        if (!IsJump) { throw new LanguageError($"Attempting to set jump destination for non-jump instruction {Type}"); }
        Second = new Val(pc);
    }

    private string DebugString => DebugPrint(" ");

    /// <summary> Converts an instruction to a string </summary>
    public string DebugPrint(string sep = "\t")
    {
        StringBuilder sb = new();
        sb.Append(Names[(int)Type]);

        if (First.IsNotNil || Type == Opcode.PushConst)
        {
            sb.Append(sep);
            sb.Append(Val.DebugPrint(First));
        }

        if (Second.IsNotNil)
        {
            sb.Append(sep);
            sb.Append(Val.DebugPrint(Second));
        }
        if (Debug != null)
        {
            sb.Append(sep);
            sb.Append("; ");
            sb.Append(Debug);
        }
        return sb.ToString();
    }

    /// <summary> Returns true if two instructions are equal </summary>
    public static bool Equal(Instruction a, Instruction b)
        => a.Type == b.Type && Val.Equals(a.First, b.First) && Val.Equals(a.Second, b.Second);
}
