using Paremnet.Data;
using Paremnet.Error;
using System.Collections.Generic;
using System.Linq;

namespace Paremnet.Core;

/// <summary>
/// Stores the results of top-level compilation: a closure that is ready for execution,
/// and a set of code block handles for code that was just compiled, for debugging purposes.
/// </summary>
public struct CompilationResults(Closure closure, List<CodeHandle> blocks)
{
    public Closure Closure = closure;
    public List<CodeHandle> Recents = blocks;
}

/// <summary>
/// Compiles source s-expression into bytecode.
/// </summary>
public class Compiler
{
    /// <summary>
    /// Compilation state for the expression being compiled
    ///
    /// <p> Val and More flags are used for tail-call optimization. "Val" is true when
    /// the expression returns a value that's then used elsewhere. "More" is false when
    /// the expression represents the final value, true if there is more to compute
    /// (this determines whether we need to jump and return, or just jump)
    ///
    /// <p> Examples, when compiling expression X:
    /// <ul>
    /// <li> val = t, more = t ... (if X y z) or (f X y)      </li>
    /// <li> val = t, more = f ... (if p X z) or (begin y X)  </li>
    /// <li> val = f, more = t ... (begin X y)                </li>
    /// <li> val = f, more = f ... impossible                 </li>
    /// </ul>
    /// </summary>
    private struct State
    {
        private bool _val, _more;

        public bool IsUnused => !_val;
        public bool IsFinal => !_more;

        public static readonly State UsedFinal = new() { _val = true, _more = false };
        public static readonly State UsedNonFinal = new() { _val = true, _more = true };
        public static readonly State NotUsedNonFinal = new() { _val = false, _more = true };
    }

    /// <summary> Label counter for each separate compilation block </summary>
    private int _labelNum = 0;

    /// <summary> Internal execution context </summary>
    private readonly Context _ctx = null;

    // some helpful symbol constants, interned only once at the beginning
    private readonly Symbol _quote;
    private readonly Symbol _begin;
    private readonly Symbol _set;
    private readonly Symbol _if;
    private readonly Symbol _ifStar;
    private readonly Symbol _while;
    private readonly Symbol _lambda;
    private readonly Symbol _defmacro;

    public Compiler(Context ctx)
    {
        Package global = ctx.Packages.Global;
        _quote = global.Intern("quote");
        _begin = global.Intern("begin");
        _set = global.Intern("set!");
        _if = global.Intern("if");
        _ifStar = global.Intern("if*");
        _while = global.Intern("while");
        _lambda = global.Intern("lambda");
        _defmacro = global.Intern("defmacro");

        _ctx = ctx;
    }

    /// <summary>
    /// Top level compilation entry point. Compiles the expression x given an empty environment.
    /// Returns the newly compiled lambda for execution, and a list of all recently
    /// compiled code blocks for debugging purposes.
    /// </summary>
    public CompilationResults Compile(Val x)
    {
        CodeHandle before = _ctx.Code.LastHandle;
        _labelNum = 0;
        Closure closure = CompileLambda(Val.NIL, new Cons(x, Val.NIL), null);
        CodeHandle after = _ctx.Code.LastHandle;

        List<CodeHandle> blocks = [];
        for (int i = before.Index + 1; i <= after.Index; i++) { blocks.Add(new CodeHandle(i)); }

        return new CompilationResults(closure, blocks);
    }

    /// <summary>
    /// Compiles the expression x, given the environment env, into a vector of instructions.
    /// </summary>
    private List<Instruction> Compile(Val x, Environment env, State st)
    {

        // check if macro
        if (IsMacroApplication(x))
        {
            return Compile(MacroExpandFull(x), env, st);
        }

        if (x.IsSymbol)
        {       // check if symbol
            return CompileVariable(x.AsSymbol, env, st);
        }

        if (x.IsAtom)
        {         // check if it's not a list
            return CompileConstant(x, st);
        }

        // it's not an atom, it's a list, deal with it.
        VerifyExpression(Cons.IsList(x), "Non-list expression detected!");
        Cons cons = x.AsConsOrNull;
        Symbol name = cons.First.AsSymbolOrNull;

        if (name == _quote)
        {    // (quote value)
            VerifyArgCount(cons, 1);
            return CompileConstant(cons.Second, st); // second element is the constant
        }
        if (name == _begin)
        {    // (begin ...)
            return CompileBegin(cons.Rest, env, st);
        }
        if (name == _set)
        {      // (set! symbol-name value)
            VerifyArgCount(cons, 2);
            VerifyExpression(cons.Second.IsSymbol, "Invalid lvalue in set!, must be a symbol, got: ", cons.Second);
            return CompileVarSet(cons.Second.AsSymbol, cons.Third, env, st);
        }
        if (name == _if)
        {       // (if pred then else) or (if pred then)
            VerifyArgCount(cons, 2, 3);
            return CompileIf(
                cons.Second,     // pred
                cons.Third,      // then
                (cons.AfterThird.IsNotNil ? cons.Fourth : Val.NIL), // else
                env, st);
        }
        if (name == _ifStar)
        {   // (if *pred else)
            VerifyArgCount(cons, 2);
            return CompileIfStar(
                cons.Second,    // pred
                cons.Third,     // else
                env, st);
        }
        if (name == _while)
        {    // (while pred body ...)
            Cons body = cons.AfterSecond.AsConsOrNull;
            return CompileWhile(cons.Second, body, env, st);
        }
        if (name == _lambda)
        {   // (lambda (args...) body...)
            if (st.IsUnused)
            {
                return null;    // it's not used, don't compile
            }
            else
            {
                Cons body = cons.AfterSecond.AsConsOrNull;
                Closure f = CompileLambda(cons.Second, body, env);
                //string debug = $"#{f.code.index} : " + Val.DebugPrint(cons.afterSecond);
                string debug = Val.DebugPrint(cons.AfterSecond);
                return Merge(
                    Emit(Opcode.MakeClosure, new Val(f), Val.NIL, debug),
                    EmitIf(st.IsFinal, Emit(Opcode.ReturnVal)));
            }
        }
        if (name == _defmacro)
        {
            return CompileAndInstallMacroDefinition(cons.Rest.AsConsOrNull, env, st);
        }

        return CompileFunctionCall(cons.First, cons.Rest.AsConsOrNull, env, st);
    }

    /// <summary>
    /// Verifies arg count of the expression (list of operands).
    /// Min and max are inclusive; default value of max (= -1) is a special value,
    /// causes max to be treated as equal to min (ie., tests for arg count == min)
    /// </summary>
    private static void VerifyArgCount(Cons cons, int min, int max = -1)
    {
        max = (max >= 0) ? max : min;  // default value means: max == min
        int count = Cons.Length(cons.Rest);
        if (count < min || count > max)
        {
            throw new CompilerError("Invalid argument count in expression " + cons +
                                    ": " + count + " supplied, expected in range [" + min + ", " + max + "]");
        }
    }

    /// <summary> Verifies that the expression is true, throws the specified error otherwise. </summary>
    private static void VerifyExpression(bool condition, string message, Val? val = null)
    {
        if (!condition)
        {
            throw new CompilerError(message + (val.HasValue ? (" " + val.Value.type) : ""));
        }
    }

    /// <summary> Returns true if the given value is a macro </summary>
    private static bool IsMacroApplication(Val x)
    {
        Cons cons = x.AsConsOrNull;
        return
            cons != null &&
            cons.First.IsSymbol &&
            cons.First.AsSymbol.Pkg.HasMacro(cons.First.AsSymbol);
    }

    /// <summary> Performs compile-time macroexpansion, one-level deep </summary>
    public Val MacroExpand1Step(Val exp)
    {
        Cons cons = exp.AsConsOrNull;
        if (cons == null || !cons.First.IsSymbol) { return exp; } // something unexpected

        Symbol name = cons.First.AsSymbol;
        Macro macro = name.Pkg.GetMacro(name);
        if (macro == null) { return exp; } // no such macro, ignore

        // now we execute the macro at compile time, in the same context...
        Val result = _ctx.Vm.Execute(macro.Body, Cons.ToNativeList(cons.Rest).ToArray());
        return result;
    }

    /// <summary> Performs compile-time macroexpansion, fully recursive </summary>
    public Val MacroExpandFull(Val exp)
    {
        Val expanded = MacroExpand1Step(exp);
        Cons cons = expanded.AsConsOrNull;
        if (cons == null || !cons.First.IsSymbol) { return expanded; } // nothing more to expand

        // if we're expanding a list, replace each element recursively
        while (cons != null)
        {
            Cons elt = cons.First.AsConsOrNull;
            if (elt != null && elt.First.IsSymbol)
            {
                Val substitute = MacroExpandFull(cons.First);
                cons.First = substitute;
            }
            cons = cons.Rest.AsConsOrNull;
        }

        return expanded;
    }

    /// <summary> Compiles a variable lookup </summary>
    private static List<Instruction> CompileVariable(Symbol x, Environment env, State st)
    {
        if (st.IsUnused) { return null; }

        VarPos pos = Environment.GetVariable(x, env);
        bool isLocal = pos.IsValid;
        return Merge(
            (isLocal ?
                Emit(Opcode.LocalGet, pos.FrameIndex, pos.SymbolIndex, Val.DebugPrint(x)) :
                Emit(Opcode.GlobalGet, x)),
            EmitIf(st.IsFinal, Emit(Opcode.ReturnVal)));
    }

    /// <summary> Compiles a constant, if it's actually used elsewhere </summary>
    private static List<Instruction> CompileConstant(Val x, State st)
    {
        if (st.IsUnused) { return null; }

        return Merge(
            Emit(Opcode.PushConst, x, Val.NIL),
            EmitIf(st.IsFinal, Emit(Opcode.ReturnVal)));
    }

    /// <summary> Compiles a sequence defined by a BEGIN - we pop all values, except for the last one </summary>
    private List<Instruction> CompileBegin(Val exps, Environment env, State st)
    {
        if (exps.IsNil)
        {
            return CompileConstant(Val.NIL, st); // (begin)
        }

        Cons cons = exps.AsConsOrNull;
        VerifyExpression(cons != null, "Unexpected value passed to begin block, instead of a cons:", exps);

        if (cons.Rest.IsNil)
        {  // length == 1
            return Compile(cons.First, env, st);
        }
        else
        {
            return Merge(
                Compile(cons.First, env, State.NotUsedNonFinal),  // note: not the final expression, set val = f, more = t
                CompileBegin(cons.Rest, env, st));
        }
    }

    /// <summary> Compiles a variable set </summary>
    private List<Instruction> CompileVarSet(Symbol x, Val value, Environment env, State st)
    {
        VarPos pos = Environment.GetVariable(x, env);
        bool isLocal = pos.IsValid;
        return Merge(
            Compile(value, env, State.UsedNonFinal),
            (isLocal ?
                Emit(Opcode.LocalSet, pos.FrameIndex, pos.SymbolIndex, Val.DebugPrint(x)) :
                Emit(Opcode.GlobalSet, x)),
            EmitIf(st.IsUnused, Emit(Opcode.StackPop)),
            EmitIf(st.IsFinal, Emit(Opcode.ReturnVal))
        );
    }

    /// <summary> Compiles an if statement (fun!) </summary>
    private List<Instruction> CompileIf(Val pred, Val then, Val els, Environment env, State st)
    {
        // (if #f x y) => y
        if (pred.IsBool && !pred.AsBool) { return Compile(els, env, st); }

        // (if #t x y) => x, or (if 5 ...) or (if "foo" ...)
        bool isConst = (pred.IsBool) || (pred.IsNumber) || (pred.IsString);
        if (isConst) { return Compile(then, env, st); }

        // actually produce the code for if/then/else clauses
        // note that those clauses will already contain a return opcode if they're final.
        List<Instruction> predCode = Compile(pred, env, State.UsedNonFinal);
        List<Instruction> thenCode = Compile(then, env, st);
        List<Instruction> elseCode = els.IsNotNil ? Compile(els, env, st) : CompileConstant(els, st);

        // (if p x x) => (begin p x)
        if (CodeEquals(thenCode, elseCode))
        {
            return Merge(
                Compile(pred, env, State.NotUsedNonFinal),
                elseCode);
        }

        // (if p x y) => p (FJUMP L1) x L1: y
        //         or    p (FJUMP L1) x (JUMP L2) L1: y L2:
        // depending on whether this is the last exp, or if there's more
        if (st.IsFinal)
        {
            string l1 = MakeLabel();
            return Merge(
                predCode,
                Emit(Opcode.JmpIfFalse, l1),
                thenCode,
                Emit(Opcode.Label, l1),
                elseCode);
        }
        else
        {
            string l1 = MakeLabel();
            string l2 = MakeLabel();
            return Merge(
                predCode,
                Emit(Opcode.JmpIfFalse, l1),
                thenCode,
                Emit(Opcode.JmpToLabel, l2),
                Emit(Opcode.Label, l1),
                elseCode,
                Emit(Opcode.Label, l2));
        }
    }

    /// <summary> Compiles an if* statement </summary>
    private List<Instruction> CompileIfStar(Val pred, Val els, Environment env, State st)
    {

        // (if* x y) will return x if it's not false, otherwise it will return y

        // (if* #f x) => x
        if (pred.IsBool && !pred.AsBool)
        {
            return Compile(els, env, st);
        }

        List<Instruction> predCode = Compile(pred, env, State.UsedNonFinal);

        State elseState = st.IsFinal ? State.UsedFinal : State.UsedNonFinal;
        List<Instruction> elseCode = els.IsNotNil ? Compile(els, env, elseState) : null;

        // (if* p x) => p (DUPE) (TJUMP L1) (POP) x L1: (POP?)
        string l1 = MakeLabel();
        return Merge(
            predCode,
            Emit(Opcode.Duplicate),
            Emit(Opcode.JmpIfTrue, l1),
            Emit(Opcode.StackPop),
            elseCode,
            Emit(Opcode.Label, l1),
            EmitIf(st.IsUnused, Emit(Opcode.StackPop)),
            EmitIf(st.IsFinal, Emit(Opcode.ReturnVal)));
    }

    /// <summary> Compiles a while loop </summary>
    private List<Instruction> CompileWhile(Val pred, Cons body, Environment env, State st)
    {
        // (while p ...) => (PUSH '()) L1: p (FJUMP L2) (POP) (begin ...) (JUMP L1) L2:
        List<Instruction> predCode = Compile(pred, env, State.UsedNonFinal);
        List<Instruction> bodyCode = CompileBegin(body, env, State.UsedNonFinal); // keep result on stack

        string l1 = MakeLabel(), l2 = MakeLabel();
        return Merge(
            Emit(Opcode.PushConst, Val.NIL),
            Emit(Opcode.Label, l1),
            predCode,
            Emit(Opcode.JmpIfFalse, l2),
            Emit(Opcode.StackPop),
            bodyCode,
            Emit(Opcode.JmpToLabel, l1),
            Emit(Opcode.Label, l2),
            EmitIf(st.IsUnused, Emit(Opcode.StackPop)),
            EmitIf(st.IsFinal, Emit(Opcode.ReturnVal)));
    }

    /// <summary> Compiles code to produce a new closure </summary>
    private Closure CompileLambda(Val args, Cons body, Environment env)
    {
        Environment newEnv = Environment.Make(MakeTrueList(args), env);
        List<Instruction> instructions = Merge(
            EmitArgs(args, newEnv.DebugPrintSymbols()),
            CompileBegin(new Val(body), newEnv, State.UsedFinal));

        string debug = newEnv.DebugPrintSymbols() + " => " + Val.DebugPrint(body);
        CodeHandle handle = _ctx.Code.AddBlock(Assemble(instructions), debug);
        return new Closure(handle, env, args.AsConsOrNull, "");
    }

    /// <summary> Compile a list, leaving all elements on the stack </summary>
    private List<Instruction> CompileList(Cons exps, Environment env) =>
        (exps == null)
            ? null
            : Merge(
                Compile(exps.First, env, State.UsedNonFinal),
                CompileList(exps.Rest.AsConsOrNull, env));

    /// <summary>
    /// Compiles a macro, and sets the given symbol to point to it. NOTE: unlike all other expressions,
    /// which are executed by the virtual machine, this happens immediately, during compilation.
    /// </summary>
    private List<Instruction> CompileAndInstallMacroDefinition(Cons cons, Environment env, State st)
    {

        // example: (defmacro foo (x) (+ x 1))
        Symbol name = cons.First.AsSymbol;
        Cons args = cons.Second.AsCons;
        Cons bodylist = cons.AfterSecond.AsConsOrNull;
        Closure body = CompileLambda(new Val(args), bodylist, env);
        Macro macro = new(name, args, body);

        // install it in the package
        name.Pkg.SetMacro(name, macro);
        return CompileConstant(Val.NIL, st);
    }

    /// <summary> Compile the application of a function to arguments </summary>
    private List<Instruction> CompileFunctionCall(Val f, Cons args, Environment env, State st)
    {
        if (f.IsCons)
        {
            Cons fcons = f.AsCons;
            if (fcons.First.IsSymbol && fcons.First.AsSymbol.FullName == "lambda" && fcons.Second.IsNil)
            {
                // ((lambda () body)) => (begin body)
                VerifyExpression(args == null, "Too many arguments supplied!");
                return CompileBegin(fcons.AfterSecond, env, st);
            }
        }

        if (st.IsFinal)
        {
            // function call as rename plus goto
            return Merge(
                CompileList(args, env),
                Compile(f, env, State.UsedNonFinal),
                Emit(Opcode.JmpClosure, Cons.Length(args)));
        }
        else
        {
            // need to save the continuation point
            string k = MakeLabel("R");
            return Merge(
                Emit(Opcode.SaveReturn, k),
                CompileList(args, env),
                Compile(f, env, State.UsedNonFinal),
                Emit(Opcode.JmpClosure, Cons.Length(args)),
                Emit(Opcode.Label, k),
                EmitIf(st.IsUnused, Emit(Opcode.StackPop)));
        }
    }

    /// <summary> Generates an appropriate ARGS or ARGSDOT sequence, making a new stack frame </summary>
    private List<Instruction> EmitArgs(Val args, string debug, int nSoFar = 0)
    {
        // recursively detect whether it's a list or ends with a dotted cons, and generate appropriate arg

        // terminal case
        if (args.IsNil) { return Emit(Opcode.MakeEnv, nSoFar, debug); }        // (lambda (a b c) ...)
        if (args.IsSymbol) { return Emit(Opcode.MakeEnvdot, nSoFar, debug); }  // (lambda (a b . c) ...)

        // if not at the end, recurse
        Cons cons = args.AsConsOrNull;
        if (cons != null && cons.First.IsSymbol) { return EmitArgs(cons.Rest, debug, nSoFar + 1); }

        throw new CompilerError("Invalid argument list");           // (lambda (a b 5 #t) ...) or some other nonsense
    }

    /// <summary> Converts a dotted cons list into a proper non-dotted one </summary>
    private Cons MakeTrueList(Val dottedList)
    {

        // we reached a terminating nil - return as is
        if (dottedList.IsNil) { return null; }

        // we reached a terminating cdr in a dotted pair - convert it
        if (dottedList.IsAtom) { return new Cons(dottedList, Val.NIL); }

        Cons cons = dottedList.AsCons;
        return new Cons(cons.First, MakeTrueList(cons.Rest)); // keep recursing
    }

    /// <summary> Generates a sequence containing a single instruction </summary>
    private static List<Instruction> Emit(Opcode type, Val first, Val second, string debug = null) =>
        [new(type, first, second, debug)];

    /// <summary> Generates a sequence containing a single instruction </summary>
    private static List<Instruction> Emit(Opcode type, Val first, string debug = null) =>
        [new(type, first, debug)];

    /// <summary> Generates a sequence containing a single instruction with no arguments </summary>
    private static List<Instruction> Emit(Opcode type) =>
        [new(type)];


    /// <summary> Creates a new unique label </summary>
    private string MakeLabel(string prefix = "L") =>
        prefix + _labelNum++.ToString();

    /// <summary> Merges sequences of instructions into a single sequence </summary>
    private static List<Instruction> Merge(params List<Instruction>[] elements) =>
        elements.Where(list => list != null).SelectMany(instr => instr).ToList();

    /// <summary> Returns the value if the condition is true, null if it's false </summary>
    private static List<Instruction> EmitIf(bool test, List<Instruction> value) => test ? value : null;

    /// <summary> Returns the value if the condition is false, null if it's true </summary>
    // private List<Instruction> EmitIfNot (bool test, List<Instruction> value) => !test ? value : null;

    /// <summary> Compares two code sequences, and returns true if they're equal </summary>
    private static bool CodeEquals(List<Instruction> a, List<Instruction> b)
    {
        if (a == null && b == null) { return true; }
        if (a == null || b == null || a.Count != b.Count) { return false; }

        for (int i = 0; i < a.Count; i++)
        {
            if (!Instruction.Equal(a[i], b[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// "Assembles" the compiled code, by resolving label references and converting them to index offsets.
    /// Modifies the code data structure in place, and returns it back to the caller.
    /// </summary>
    private static List<Instruction> Assemble(List<Instruction> code)
    {
        LabelPositions positions = new(code);

        foreach (var inst in code)
        {
            if (inst.IsJump)
            {
                int pos = positions.FindPosition(inst.First);
                if (pos >= 0)
                {
                    inst.UpdateJumpDestination(pos);
                }
                else
                {
                    throw new CompilerError($"Can't find jump label {inst.First} during assembly");
                }
            }
        }

        return code;
    }

    /// <summary>
    /// Temporary data structure used during assembly: holds code positions for all labels
    /// </summary>
    private class LabelPositions : Dictionary<string, int>
    {
        public LabelPositions(List<Instruction> code)
        {
            for (int i = 0; i < code.Count; i++)
            {
                Instruction inst = code[i];
                if (inst.Type == Opcode.Label)
                {
                    string label = inst.First.AsString;
                    this[label] = i;
                }
            }
        }

        /// <summary> Returns code position of the given label, or -1 if not found or the value is not a label. </summary>
        public int FindPosition(Val label)
        {
            if (!label.IsString) { return -1; }
            return TryGetValue(label.AsString, out int pos) ? pos : -1;
        }
    }

}
