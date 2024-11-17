using Paremnet.Data;
using Paremnet.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Paremnet.Core;

public class Primitives
{
    private static int _gensymIndex = 1;

    private static Dictionary<string, List<Primitive>> ALL_PRIMITIVES_DICT = new();

    private static readonly List<Primitive> ALL_PRIMITIVES_VECTOR = new() {
        new Primitive("+", 2, new Function((Context ctx, Val a, Val b) => ValAdd(a, b))),
        new Primitive("-", 2, new Function((Context ctx, Val a, Val b) => ValSub(a, b))),
        new Primitive("*", 2, new Function((Context ctx, Val a, Val b) => ValMul(a, b))),
        new Primitive("/", 2, new Function((Context ctx, Val a, Val b) => ValDiv(a, b))),

        new Primitive("+", 3, new Function((Context ctx, VarArgs args) =>
            FoldLeft((a, b) => ValAdd(a, b), 0, args)), FnType.VarArgs),
        new Primitive("*", 3, new Function((Context ctx, VarArgs args) =>
            FoldLeft((a, b) => ValMul(a, b), 1, args)), FnType.VarArgs),

        new Primitive("=",  2, new Function((Context ctx, Val a, Val b) => Val.Equals(a, b))),
        new Primitive("!=", 2, new Function((Context ctx, Val a, Val b) => ! Val.Equals(a, b))),
        new Primitive("<",  2, new Function((Context ctx, Val a, Val b) => ValLT(a, b))),
        new Primitive("<=", 2, new Function((Context ctx, Val a, Val b) => ValLTE(a, b))),
        new Primitive(">",  2, new Function((Context ctx, Val a, Val b) => ValGT(a, b))),
        new Primitive(">=", 2, new Function((Context ctx, Val a, Val b) => ValGTE(a, b))),

        new Primitive("cons", 2, new Function((Context ctx, Val a, Val b) => new Cons(a, b))),
        new Primitive("list", 0, new Function((ctx) => Val.NIL)),
        new Primitive("list", 1, new Function((Context ctx, Val a) => new Cons(a, Val.NIL))),
        new Primitive("list", 2, new Function((Context ctx, Val a, Val b) => new Cons(a, new Cons(b, Val.NIL)))),
        new Primitive("list", 3, new Function((Context ctx, VarArgs args) => args.AsVal), FnType.VarArgs),

        new Primitive("append", 1, new Function((Context ctx, VarArgs args) =>
            FoldRight(AppendHelper, Val.NIL, args)), FnType.VarArgs),

        new Primitive("length", 1, new Function((Context ctx, Val a) => Cons.Length(a))),

        new Primitive("not", 1, new Function((Context ctx, Val a) => !a.CastToBool)),
        new Primitive("null?", 1, new Function((Context ctx, Val a) => a.IsNil)),
        new Primitive("cons?", 1, new Function((Context ctx, Val a) => a.IsCons)),
        new Primitive("string?", 1, new Function((Context ctx, Val a) => a.IsString)),
        new Primitive("number?", 1, new Function((Context ctx, Val a) => a.IsNumber)),
        new Primitive("boolean?", 1, new Function((Context ctx, Val a) => a.IsBool)),
        new Primitive("atom?", 1, new Function((Context ctx, Val a) => !a.IsCons)),
        new Primitive("closure?", 1, new Function((Context ctx, Val a) => a.IsClosure)),

        new Primitive("car", 1, new Function((Context ctx, Val a) => a.AsCons.first)),
        new Primitive("cdr", 1, new Function((Context ctx, Val a) => a.AsCons.rest)),
        new Primitive("cadr", 1, new Function((Context ctx, Val a) => a.AsCons.second)),
        new Primitive("cddr", 1, new Function((Context ctx, Val a) => a.AsCons.afterSecond)),
        new Primitive("caddr", 1, new Function((Context ctx, Val a) => a.AsCons.third)),
        new Primitive("cdddr", 1, new Function((Context ctx, Val a) => a.AsCons.afterThird)),

        new Primitive("nth", 2, new Function((Context ctx, Val a, Val n) => a.AsCons.GetNth(n.AsInt))),
        new Primitive("nth-tail", 2, new Function((Context ctx, Val a, Val n) => a.AsCons.GetNthTail(n.AsInt))),
        new Primitive("nth-cons", 2, new Function((Context ctx, Val a, Val n) => a.AsCons.GetNthCons(n.AsInt))),

        new Primitive("map", 2, new Function((Context ctx, Val a, Val b) => {
            Closure fn = a.AsClosure;
            Cons list = b.AsCons;
            return new Val(MapHelper(ctx, fn, list));
        }), sideFx: SideFx.Possible),

        // macroexpansion
        new Primitive("mx1", 1, new Function((ctx, exp) => ctx.compiler.MacroExpand1Step(exp))),
        new Primitive("mx", 1, new Function((ctx, exp) => ctx.compiler.MacroExpandFull(exp))),

        // helpers
        new Primitive("trace", 1, new Function((Context ctx, VarArgs args) => {
            List<Val> arglist = args.ToNativeList();
            Console.WriteLine(string.Join(" ", arglist.Select(val => Val.Print(val))));
            return Val.NIL;
        }), FnType.VarArgs, SideFx.Possible),

        new Primitive("gensym", 0, new Function((ctx) => GensymHelper(ctx, "GENSYM-"))),
        new Primitive("gensym", 1, new Function((Context ctx, Val a) => GensymHelper(ctx, a.AsStringOrNull))),

        new Primitive("eval", 1, new Function((Context ctx, Val e) => {
            CompilationResults closure = ctx.compiler.Compile(e);
            Val result = ctx.vm.Execute(closure.closure);
            return result;
        }), sideFx: SideFx.Possible),

        // packages
        new Primitive("package-set", 1, new Function((Context ctx, Val a) => {
            string name = a.IsNil ? null : a.AsString; // nil package name == global package
            Package pkg = ctx.packages.Intern(name);
            ctx.packages.current = pkg;
            return a.IsNil ? Val.NIL : new Val(name);
        }), sideFx: SideFx.Possible),

        new Primitive("package-get", 0, new Function (ctx => new Val(ctx.packages.current.name)),
            sideFx: SideFx.Possible),

        new Primitive("package-import", 1, new Function ((Context ctx, VarArgs names) => {
            foreach (Val a in names.ToNativeList()) {
                string name = a.IsNil ? null : a.AsString;
                ctx.packages.current.AddImport(ctx.packages.Intern(name));
            }
            return Val.NIL;
        }), FnType.VarArgs, SideFx.Possible),

        new Primitive("package-imports", 0, new Function (ctx => {
            List<Val> imports = ctx.packages.current.ListImports();
            return Cons.MakeList(imports);
        }), sideFx: SideFx.Possible),

        new Primitive("package-export", 1, new Function ((Context ctx, Val a) => {
            Cons names = a.AsConsOrNull;
            while (names != null) {
                Symbol symbol = names.first.AsSymbol;
                symbol.exported = true;
                names = names.rest.AsConsOrNull;
            }
            return Val.NIL;
        }), sideFx: SideFx.Possible),

        new Primitive("package-exports", 0, new Function (ctx => {
            List<Val> exports = ctx.packages.current.ListExports();
            return Cons.MakeList(exports);
        }), sideFx: SideFx.Possible),

        new Primitive("error", 1, new Function ((Context ctx, VarArgs names) => {
            string[] all = names.ToNativeList().Select(v => v.AsStringOrNull ?? Val.Print(v)).ToArray();
            throw new RuntimeError(all);
        }), argsType: FnType.VarArgs, sideFx: SideFx.Possible),

        // .net interop

        // generic dotdot function that reads fields/properties and calls functions
        // (.. 'System.DateTime)     => returns type
        // (.. 'System.DateTime.Now) => returns value of the property or field
        // (.. mydata 'ToString "D") => calls mydata.ToString("D")
        // (.. myarray 'Item 0)      => returns 0th item etc.
        // (.. mydata 'ToString "D" 'Length 'ToString) => calls mydata.ToString("D").Length.ToString()
        new Primitive ("..", 1, new Function(Interop.DotDot), FnType.VarArgs, SideFx.Possible),

        // (.new 'System.DateTime 1999 12 31)
        // (.new (.. 'System 'DateTime) 1999 12 31)
        new Primitive(".new", 1, new Function((Context ctx, VarArgs args) => {
            var (type, varargs) = ParseArgsForConstructorInterop(args);
            if (type == null) { return Val.NIL; }

            return Val.TryUnbox(TypeUtils.Instantiate(type, varargs));
        }), FnType.VarArgs, SideFx.Possible),

        // (.! some-instance 'FieldName 42)
        new Primitive (".!", 3, new Function((Context ctx, VarArgs args) => {
            var (instance, type, memberName, targetValue, _) = ParseSetterArgs(args, true);

            MemberInfo member = TypeUtils.GetFieldOrProp(type, memberName, true);
            TypeUtils.SetValue(member, instance, targetValue.AsBoxedValue);
            return targetValue;
        }), sideFx: SideFx.Possible),

        // (.! some-instance 'Item 0 "hello") sets 0th item etc
        new Primitive (".!", 4, new Function((Context ctx, VarArgs args) => {
            var (instance, type, memberName, index, targetValue) = ParseSetterArgs(args, true);

            MemberInfo member = TypeUtils.GetFieldOrProp(type, memberName, true);
            TypeUtils.SetValue(member, instance, targetValue.AsBoxedValue, new object[] { index.AsBoxedValue });
            return targetValue;
        }), sideFx: SideFx.Possible),


        //
        // constant-time vectors

        // (make-vector 3)
        // (make-vector '(1 2 3))
        new Primitive("make-vector", 1, new Function((Context ctx, Val arg) => {
            if (arg.IsInt) { return new Val(new Vector(Enumerable.Repeat(Val.NIL, arg.AsInt))); }
            if (arg.IsCons) { return new Val(new Vector(arg.AsCons)); }
            throw new LanguageError("Invalid parameter, expected size or list, got: " + arg.ToString());
        })),

        // (make-vector 3 "value")
        new Primitive("make-vector", 2, new Function((Context ctx, Val count, Val val) => {
            if (count.IsInt) { return new Val(new Vector(Enumerable.Repeat(val, count.AsInt))); }
            throw new LanguageError("Invalid parameter, expected size, got: " + count.ToString());
        })),

        new Primitive("vector?", 1, new Function((Context ctx, Val arg) => {
            return arg.IsVector;
        })),

        new Primitive("vector-length", 1, new Function((Context ctx, Val v) => {
            Vector vector = v.AsVectorOrNull;
            if (vector == null) { throw new LanguageError("Value is not a vector"); }
            return vector.Count;
        })),

        new Primitive("vector-get", 2, new Function((Context ctx, Val v, Val i) => {
            Vector vector = v.AsVectorOrNull;
            int index = i.AsInt;
            if (vector == null) { throw new LanguageError("Value is not a vector"); }
            if (index < 0 || index >= vector.Count) { throw new LanguageError($"Index value {index} out of bounds"); }
            return vector[index];
        })),

        new Primitive("vector-set!", 3, new Function((Context ctx, Val v, Val i, Val value) => {
            Vector vector = v.AsVectorOrNull;
            int index = i.AsInt;
            if (vector == null) { throw new LanguageError("Value is not a vector"); }
            if (index < 0 || index >= vector.Count) { throw new LanguageError($"Index value {index} out of bounds"); }
            vector[index] = value;
            return value;
        }), sideFx: SideFx.Possible),
    };

    /// <summary>
    /// If f is a symbol that refers to a primitive, and it's not shadowed in the local environment,
    /// returns an appropriate instance of Primitive for that argument count.
    /// </summary>
    public static Primitive FindGlobal(Val f, Data.Environment env, int nargs) =>
        (f.IsSymbol && Data.Environment.GetVariable(f.AsSymbol, env).IsNotValid) ?
            FindNary(f.AsSymbol.name, nargs) :
            null;

    /// <summary> Helper function, searches based on name and argument count </summary>
    public static Primitive FindNary(string symbol, int nargs)
    {
        List<Primitive> primitives = ALL_PRIMITIVES_DICT[symbol];
        foreach (Primitive p in primitives)
        {
            if (symbol == p.name && (p.IsExact ? nargs == p.minargs : nargs >= p.minargs))
            {
                return p;
            }
        }
        return null;
    }

    /// <summary> Initializes the core package with stub functions for primitives </summary>
    public static void InitializeCorePackage(Context context, Package pkg)
    {
        // clear out and reinitialize the dictionary.
        // also, intern all primitives in their appropriate package
        ALL_PRIMITIVES_DICT = new Dictionary<string, List<Primitive>>();

        foreach (Primitive p in ALL_PRIMITIVES_VECTOR)
        {
            // dictionary update
            if (!ALL_PRIMITIVES_DICT.TryGetValue(p.name, out List<Primitive> v))
            {
                v = ALL_PRIMITIVES_DICT[p.name] = new List<Primitive>();
            }

            // add to the list of primitives of that name
            v.Add(p);

            // also intern in package, if it hasn't been interned yet
            if (pkg.Find(p.name, false) == null)
            {
                Symbol name = pkg.Intern(p.name);
                name.exported = true;
                List<Instruction> instructions = new() {
                    new Instruction(Opcode.CALL_PRIMOP, p.name),
                    new Instruction(Opcode.RETURN_VAL)};

                CodeHandle code = context.code.AddBlock(instructions, name.fullName);
                pkg.SetValue(name, new Closure(code, null, null, name.fullName));
            }
        }
    }

    /// <summary> Performs the append operation on two lists, by creating a new cons
    /// list that copies elements from the first value, and its tail is the second value </summary>
    private static Val AppendHelper(Val aval, Val bval)
    {
        Cons alist = aval.AsConsOrNull;
        Cons head = null, current = null, previous = null;

        // copy all nodes from a, set cdr of the last one to b
        while (alist != null)
        {
            current = new Cons(alist.first, Val.NIL);
            if (head == null) { head = current; }
            if (previous != null) { previous.rest = current; }
            previous = current;
            alist = alist.rest.AsConsOrNull;
        }

        if (current != null)
        {
            // a != () => head points to the first new node
            current.rest = bval;
            return head;
        }
        else
        {
            // a == (), we should return b
            return bval;
        }
    }

    /// <summary> Generates a new symbol </summary>
    private static Val GensymHelper(Context ctx, string prefix)
    {
        while (true)
        {
            string gname = prefix + _gensymIndex;
            _gensymIndex++;
            Package current = ctx.packages.current;
            if (current.Find(gname, false) == null)
            {
                return new Val(current.Intern(gname));
            }
        };
    }

    /// <summary> Maps a function over elements of the list, and returns a new list with the results </summary>
    private static Cons MapHelper(Context ctx, Closure fn, Cons list)
    {
        Cons head = null;
        Cons previous = null;

        // apply fn over all elements of the list, making a copy as we go
        while (list != null)
        {
            Val input = list.first;
            Val output = ctx.vm.Execute(fn, input);
            Cons current = new(output, Val.NIL);
            if (head == null) { head = current; }
            if (previous != null) { previous.rest = current; }
            previous = current;
            list = list.rest.AsConsOrNull;
        }

        return head;
    }

    ///// <summary> Performs a left fold on the array: +, 0, [1, 2, 3] => (((0 + 1) + 2) + 3) </summary>
    private static Val FoldLeft(Func<Val, Val, Val> fn, Val baseElement, VarArgs args)
    {
        Val result = baseElement;
        List<Val> elements = args.ToNativeList();
        for (int i = 0, len = elements.Count; i < len; i++)
        {
            result = fn(result, elements[i]);
        }
        return result;
    }

    ///// <summary> Performs a right fold on the array: +, 0, [1, 2, 3] => (1 + (2 + (3 + 0))) </summary>
    private static Val FoldRight(Func<Val, Val, Val> fn, Val baseElement, VarArgs args)
    {
        Val result = baseElement;
        List<Val> elements = args.ToNativeList();
        for (int i = elements.Count - 1; i >= 0; i--)
        {
            result = fn(elements[i], result);
        }
        return result;
    }

    private static Val ValAdd(Val a, Val b)
    {
        if (a.IsInt && b.IsInt) { return new Val(a.AsInt + b.AsInt); }
        if (a.IsNumber && b.IsNumber) { return new Val(a.CastToFloat + b.CastToFloat); }
        throw new LanguageError("Add applied to non-numbers");
    }

    private static Val ValSub(Val a, Val b)
    {
        if (a.IsInt && b.IsInt) { return new Val(a.AsInt - b.AsInt); }
        if (a.IsNumber && b.IsNumber) { return new Val(a.CastToFloat - b.CastToFloat); }
        throw new LanguageError("Add applied to non-numbers");
    }

    private static Val ValMul(Val a, Val b)
    {
        if (a.IsInt && b.IsInt) { return new Val(a.AsInt * b.AsInt); }
        if (a.IsNumber && b.IsNumber) { return new Val(a.CastToFloat * b.CastToFloat); }
        throw new LanguageError("Add applied to non-numbers");
    }

    private static Val ValDiv(Val a, Val b)
    {
        if (a.IsInt && b.IsInt) { return new Val(a.AsInt / b.AsInt); }
        if (a.IsNumber && b.IsNumber) { return new Val(a.CastToFloat / b.CastToFloat); }
        throw new LanguageError("Add applied to non-numbers");
    }

    private static Val ValLT(Val a, Val b) => a.CastToFloat < b.CastToFloat;

    private static Val ValLTE(Val a, Val b) => a.CastToFloat <= b.CastToFloat;

    private static Val ValGT(Val a, Val b) => a.CastToFloat > b.CastToFloat;

    private static Val ValGTE(Val a, Val b) => a.CastToFloat >= b.CastToFloat;

    //
    // helpers for .net interop

    /// <summary> Extract a name from either the symbol or the string value of a val </summary>
    private static string GetStringOrSymbolName(Val v) => v.AsStringOrNull ?? v.AsSymbolOrNull?.name;

    /// <summary>
    /// Extract a .net type descriptor from the argument, which could be either the type itself
    /// wrapped in a val, or a fully-qualified name, either as a symbol or a string,
    /// or finally an object and we need to look up its type at runtime.
    /// </summary>
    private static Type GetTypeFromNameOrObject(Val value)
    {
        if (value.IsObject && value.AsObject is Type t) { return t; }

        string name = GetStringOrSymbolName(value);
        if (name != null) { return TypeUtils.GetType(name); }

        return value.AsBoxedValue?.GetType();
    }

    /// <summary>
    /// Given a list of args (during a new instance call), parse out the first one as the class name,
    /// and convert the rest into an object array suitable for passing through reflection.
    /// </summary>
    private static (Type type, object[] varargs) ParseArgsForConstructorInterop(VarArgs args)
    {
        Cons list = args.cons;
        Val first = list?.first ?? Val.NIL;

        Type type = GetTypeFromNameOrObject(first);
        object[] varargs = TurnConsIntoBoxedArray(list?.rest);
        return (type, varargs);
    }

    /// <summary>
    /// Given a list of args (during a method search), parse out the first one as the name class,
    /// second as method name, and convert the rest into an object array suitable for passing through reflection.
    /// </summary>
    private static (Type type, string member, object[] varargs) ParseArgsForMethodSearch(VarArgs args)
    {
        Cons list = args.cons;
        Val first = list?.first ?? Val.NIL;
        Val second = list?.second ?? Val.NIL;

        Type type = GetTypeFromNameOrObject(first);
        string member = GetStringOrSymbolName(second);
        object[] varargs = TurnConsIntoBoxedArray(list?.afterSecond);
        return (type, member, varargs);
    }

    /// <summary>
    /// Given a list of args (for a function call), parse out the first one as the type we're referring to,
    /// second as method name, and convert the rest into an object array suitable for passing through reflection.
    /// </summary>
    private static (MethodInfo method, object instance, object[] varargs) ParseArgsForMethodCall(VarArgs args)
    {
        Cons list = args.cons;
        Val first = list?.first ?? Val.NIL;
        Val second = list?.second ?? Val.NIL;

        object instance = first.AsObjectOrNull;
        MethodInfo method = second.GetObjectOrNull<MethodInfo>();
        object[] varargs = TurnConsIntoBoxedArray(list?.afterSecond);
        return (method, instance, varargs);
    }

    /// <summary>
    /// Parse out the first argument as a type based on name or instance,
    /// and the second as a member that's either a field or a property field.
    /// </summary>
    private static (Type type, string member) ParseArgsForMemberSearch(VarArgs args)
    {
        Cons list = args.cons;
        Val first = list?.first ?? Val.NIL;
        Val second = list?.second ?? Val.NIL;

        Type type = GetTypeFromNameOrObject(first);
        string member = GetStringOrSymbolName(second);
        return (type, member);
    }

    /// <summary>
    /// Given an instance as the first argument, parse out its type,
    /// and parse the second arg as a member that's either a field or a property field.
    /// If the setter flag is set, it also parses out the third element as the new value.
    /// </summary>
    private static (object instance, Type type, string member, Val third, Val fourth) ParseSetterArgs(VarArgs args, bool setter)
    {
        Cons list = args.cons;
        Val first = list?.first ?? Val.NIL;
        Val second = list?.second ?? Val.NIL;
        Val third = (setter && list != null) ? list.third : Val.NIL;
        Val fourth = (setter && list != null && list.afterThird.IsNotNil) ? list.fourth : Val.NIL;

        object instance = first.AsBoxedValue;
        Type type = instance?.GetType();
        string member = GetStringOrSymbolName(second);
        return (instance, type, member, third, fourth);
    }

    private static object[] TurnConsIntoBoxedArray(Val? cons) =>
        cons?.AsConsOrNull?.ToNativeList().Select(v => v.AsBoxedValue).ToArray() ?? Array.Empty<object>();

    /// <summary> Collapses a native path (expressed as a Cons list) into a fully qualified name </summary>
    /*
    private static string CollapseIntoNativeName (Cons path) {
        string name = "";
        while (path != null) {
            if (name.Length > 0) { name += "."; }
            name += path.first.AsSymbol.name;
            path = path.rest.AsCons;
        }
        return name;
    }
    */
}