using Paremnet.Core;
using Paremnet.Data;
using Paremnet.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Paremnet
{
    public class TestClass
    {
        public int MyIntField = 42;
        public int MyIntGetter => MyIntField;
        public int MyIntProperty { get; set; }

        public string MyStringField = "hello";
        public int MyFn(int x) => x + MyIntField;
    }

    namespace Inner
    {
        public class TestInner
        {
            public static int staticField = 42;

            public static int StaticProperty
            {
                get => staticField;
                set => staticField = value;
            }

            public static int StaticFn(int x) => x + staticField;
        }
    }


    public struct TestStruct
    {
        public int MyIntField;
        public int MyIntGetter => MyIntField;
    }

    public class UnitTests
    {
        public enum LogType { None, Console, TempFile }

        /// <summary> Failures count during the last test run. </summary>
        private int _failures = 0;

        /// <summary> Where are we logging unit test results? </summary>
        public static readonly LogType LogTarget = LogType.TempFile; // change as needed

        /// <summary> Logging implementation, could target standard console, or file, or nothing </summary>
        private class Logger : ILogger
        {
            private TextWriter _writer;

            public bool EnableParsingLogging => true;
            public bool EnableInstructionLogging => true;
            public bool EnableStackLogging => true;

            public void OpenLog(string name)
            {
                switch (LogTarget)
                {
                    case LogType.TempFile:
                        string testDir = Path.Combine("..", "..", "..", "TestResults");
                        Directory.CreateDirectory(testDir);
                        string filePath = Path.Combine(testDir, $"{name}.txt");
                        _writer = new StreamWriter(new FileStream(filePath, FileMode.Create));
                        _writer.WriteLine($"TEST {name}");
                        break;

                    case LogType.Console:
                        _writer = System.Console.Out;
                        break;

                    default:
                        _writer = null; // don't log
                        break;
                }
            }

            public void Log(params object[] args)
            {
                if (_writer == null) { return; } // drop on the floor

                IEnumerable<string> strings = args.Select(obj => (obj == null) ? "null" : obj.ToString());
                string message = string.Join(" ", strings);
                _writer.WriteLine(message);
            }

            public void CloseLog()
            {
                _writer.Flush();
                _writer.Close();
                _writer = null;
            }
        }

        /// <summary> Logger implementation </summary>
        private readonly Logger _logger = new();

        /// <summary> Simple logger wrapper </summary>
        private void Log(params object[] args)
        {
            if (_logger != null)
            {
                _logger.Log(args);
            }
        }

        [Fact]
        public void RunTests()
        {
            Run(PrintAllStandardLibraries);
            return;

            void Run(System.Action fn)
            {
                _logger.OpenLog(fn.GetMethodInfo().Name);
                _failures = 0;
                fn();
                Log(_failures == 0 ? "SUCCESS" : $"FAILURES: {_failures}");
                _logger.CloseLog();
            }
        }

        private void DumpCodeBlocks(Context ctx) => Log(ctx.Code.DebugPrintAll());

        [Fact]
        public void Nil()
        {
            Assert.True(Val.NIL.IsAtom);
            Assert.False(Val.NIL.IsCons);
            Assert.True(Val.NIL.IsNil);
            Assert.Equal(Val.NIL, Val.NIL);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(System.SByte.MinValue)]
        [InlineData(System.SByte.MaxValue)]
        public void Int8(System.SByte value)
        {
            Val a = value, b = value;
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.True(a.IsAtom);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(System.Byte.MinValue)]
        [InlineData(System.Byte.MaxValue)]
        public void UInt8(System.Byte value)
        {
            Val a = value, b = value;
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.True(a.IsAtom);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(System.Int16.MinValue)]
        [InlineData(System.Int16.MaxValue)]
        public void Int16(System.Int16 value)
        {
            Val a = value, b = value;
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.True(a.IsAtom);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(System.UInt16.MinValue)]
        [InlineData(System.UInt16.MaxValue)]
        public void UInt16(System.UInt16 value)
        {
            Val a = value, b = value;
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.True(a.IsAtom);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(System.Int32.MinValue)]
        [InlineData(System.Int32.MaxValue)]
        public void Int32(System.Int32 value)
        {
            Val a = value, b = value;
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.True(a.IsAtom);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(System.UInt32.MinValue)]
        [InlineData(System.UInt32.MaxValue)]
        public void UInt32(System.UInt32 value)
        {
            Val a = value, b = value;
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.True(a.IsAtom);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(System.Int64.MinValue)]
        [InlineData(System.Int64.MaxValue)]
        public void Int64(System.Int64 value)
        {
            Val a = value, b = value;
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.True(a.IsAtom);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(System.UInt64.MinValue)]
        [InlineData(System.UInt64.MaxValue)]
        public void UInt64(System.UInt64 value)
        {
            Val a = value, b = value;
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.True(a.IsAtom);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        // [InlineData(System.Int128.MinValue)]
        // [InlineData(System.Int128.MaxValue)]
        public void Int128(System.Int128 value)
        {
            Val a = value, b = value;
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.True(a.IsAtom);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        // [InlineData(System.UInt128.MinValue)]
        // [InlineData(System.UInt128.MaxValue)]
        public void UInt128(System.UInt128 value)
        {
            Val a = new(value), b = new(value);
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.True(a.IsAtom);
        }

        [Fact]
        public void TestConsAndAtoms()
        {
            { // TODO: Empty list
            }

            { // Proper list
                Val c = new Cons(1, Val.NIL);
                Assert.True(c.IsCons);
                Assert.Equal(1, Cons.Length(c));
            }

            { // Cell
                Val c = new Cons(1, 2);
                Assert.True(c.IsCons);
                // FIXME: This is not a list and shouldn't return a length
                //Assert.Throws<System.Exception>(() => Cons.Length(c));
            }

            Assert.True(new Val(new Cons(new Val(1), new Val(2))).IsCons);

            {
                Cons list1 = new("foo", new Cons("bar", Val.NIL));
                Assert.True(Cons.IsCons(list1));
                Assert.True(Cons.IsList(list1));
                Assert.Equal(2, Cons.Length(list1));
                Assert.Equal("foo", list1.First);
                Assert.Equal("bar", list1.Second);
                Assert.Equal(Val.NIL, list1.AfterSecond);
                Assert.True(list1.First.IsAtom); // "foo"
                Assert.True(list1.Rest.IsCons); // ("bar")
                Assert.True(list1.Second.IsAtom); // "bar"
                Assert.True(list1.AfterSecond.IsAtom); // nil
                Assert.True(list1.AfterSecond.IsNil); // null
                Assert.Equal("(\"foo\" \"bar\")", Val.Print(list1));
            }

            {
                Cons list2 = Cons.MakeList("foo", "bar");
                Assert.True(Cons.IsCons(list2));
                Assert.True(Cons.IsList(list2));
                Assert.Equal(2, Cons.Length(list2));
                Assert.Equal("foo", list2.First);
                Assert.Equal("bar", list2.Second);
                Assert.Equal(Val.NIL, list2.AfterSecond);
                Assert.True(list2.First.IsAtom); // "foo"
                Assert.True(list2.Rest.IsCons); // ("bar")
                Assert.True(list2.Second.IsAtom); // "bar"
                Assert.True(list2.AfterSecond.IsAtom); // null
                Assert.True(list2.AfterSecond.IsNil); // null
                Assert.Equal("(\"foo\" \"bar\")", Val.Print(list2));
            }

            {
                Cons nonlist = new("foo", "bar");
                Assert.True(Cons.IsCons(nonlist));
                Assert.False(Cons.IsList(nonlist));
                Assert.Equal("foo", nonlist.First);
                Assert.Equal("bar", nonlist.Rest);
                Assert.True(nonlist.First.IsAtom); // "foo"
                Assert.True(nonlist.Rest.IsAtom); // "bar"
                Assert.True(nonlist.Rest.IsNotNil);
                Assert.Equal("(\"foo\" . \"bar\")", Val.Print(nonlist));
            }
        }

        [Fact]
        public void TestPackagesAndSymbols()
        {
            Packages packages = new();
            Package p = packages.Global;   // global package

            Symbol foo = p.Intern("foo");
            Assert.Equal("foo", foo.Name);
            Assert.Equal(p, foo.Pkg);
            Assert.Equal("foo", foo.FullName);
            Assert.True(p.Intern("foo") == foo); // make sure interning returns the same instance
            Assert.True(p.Unintern(foo.Name));        // first one removes successfully
            Assert.False(p.Unintern(foo.Name));      // but second time there is nothing to remove
            Assert.True(p.Intern("foo") != foo); // since we uninterned, second interning will return a different one

            Package p2 = new("fancy"); // some fancy package
            Symbol foo2 = p2.Intern("foo");
            Assert.Equal("foo", foo2.Name);
            Assert.Equal(p2, foo2.Pkg);
            Assert.Equal("fancy:foo", foo2.FullName);

            // test the packages list

            Assert.Equal((string)null, packages.Global.Name); // get the global package
            Assert.Equal("foo", Val.Print(packages.Global.Intern("foo"))); // check symbol name
            Assert.Equal("", packages.Keywords.Name);      // get the keywords package
            Assert.Equal(":foo", Val.Print(packages.Keywords.Intern("foo")));  // check symbol name

            Assert.Equal((Package)null, packages.Find("fancy"));    // make sure the fancy package was not added yet
            Assert.Equal(p2, packages.Add(p2));            // add our fancy custom package
            Assert.Equal(packages.Intern("fancy"), p2);    // get the fancy package - should be the same one
            Assert.Equal("fancy:foo", Val.Print(packages.Intern("fancy").Intern("foo")));  // check symbol name
            Assert.True(packages.Remove(p2));             // check removal (should only return true the first time)
            Assert.True(!packages.Remove(p2));            // check removal (should only return true the first time)
        }

        [Fact]
        public void TestEnvironments()
        {
            // test environments

            Package p = new("temp");
            Environment e2 = Environment.Make(Cons.MakeList(p.Intern("env2symbol0")), null);
            // e2.setAt(0, p.intern("env2symbol0"));
            Environment e1 = Environment.Make(Cons.MakeList(p.Intern("env1symbol0"), p.Intern("env1symbol1")), e2);
            // e1.setAt(0, p.intern("env1symbol0"));
            // e1.setAt(1, p.intern("env1symbol1"));
            Environment e0 = Environment.Make(Cons.MakeList(p.Intern("env0symbol0"), p.Intern("env0symbol1")), e1);
            // e0.setAt(0, p.intern("env0symbol0"));
            // e0.setAt(1, p.intern("env0symbol1"));
            Assert.Equal(2, Environment.GetVariable(p.Intern("env2symbol0"), e0).FrameIndex); // get frame coord
            Assert.Equal(0, Environment.GetVariable(p.Intern("env2symbol0"), e0).SymbolIndex); // get symbol coord
            Assert.Equal(1, Environment.GetVariable(p.Intern("env1symbol1"), e0).FrameIndex); // get frame coord
            Assert.Equal(1, Environment.GetVariable(p.Intern("env1symbol1"), e0).SymbolIndex); // get symbol coord
            Assert.Equal(0, Environment.GetVariable(p.Intern("env0symbol0"), e0).FrameIndex); // get frame coord
            Assert.Equal(0, Environment.GetVariable(p.Intern("env0symbol0"), e0).SymbolIndex); // get symbol coord

            VarPos e2S0Loc = Environment.GetVariable(p.Intern("env2symbol0"), e0);
            Assert.Equal(p.Intern("env2symbol0"), Environment.GetSymbolAt(e2S0Loc, e0));
            Environment.SetSymbolAt(e2S0Loc, p.Intern("NEW_SYMBOL"), e0);
            Assert.Equal(p.Intern("NEW_SYMBOL"), Environment.GetSymbolAt(e2S0Loc, e0));
            Assert.Equal(2, Environment.GetVariable(p.Intern("NEW_SYMBOL"), e0).FrameIndex); // get frame coord
            Assert.Equal(0, Environment.GetVariable(p.Intern("NEW_SYMBOL"), e0).SymbolIndex); // get symbol coord
        }

        /// <summary> Tests the character stream </summary>
        [Fact]
        public void TestCharStream()
        {
            // first, test the stream wrapper
            InputStream stream = new();
            stream.Add("foo");
            stream.Save();
            Assert.False(stream.IsEmpty);
            Assert.Equal('f', stream.Peek()); // don't remove
            Assert.Equal('f', stream.Read()); // remove
            Assert.Equal('o', stream.Peek()); // don't remove
            Assert.Equal('o', stream.Read()); // remove
            Assert.Equal('o', stream.Read()); // remove last one
            Assert.Equal((char)0, stream.Read());
            Assert.True(stream.IsEmpty);
            Assert.True(stream.Restore());   // make sure we can restore the old save
            Assert.Equal('f', stream.Peek()); // we're back at the beginning
            Assert.False(stream.Restore()); // there's nothing left to restore
        }

        /// <summary> Tests the parser part of the system </summary>
        [Fact]
        public void TestParser()
        {
            Packages packages = new();
            Parser p = new(packages, _logger);

            // test parsing simple atoms, check their internal form
            Assert.Equal(1, ParseRaw(p, "1"));
            Assert.Equal(1.1f, ParseRaw(p, "+1.1"));
            Assert.Equal(-2f, ParseRaw(p, "-2.0"));
            Assert.Equal(-2, ParseRaw(p, "-2"));
            Assert.Equal(true, ParseRaw(p, "#t"));
            Assert.Equal(false, ParseRaw(p, "#f"));
            Assert.Equal(false, ParseRaw(p, "#hashwhatever"));
            Assert.Equal(packages.Global.Intern("a"), ParseRaw(p, "a"));
            Assert.Equal(Val.NIL, ParseRaw(p, "()"));
            Assert.Equal("foo \" ", ParseRaw(p, "\"foo \\\" \""));

            {
                Val actual = ParseRaw(p, "^System.Byte 1");
                Assert.Equal((byte)1, actual.vuint8);
                var key = new Val(new Symbol("type", new Package(Packages.NameKeywords)));
                Assert.Equal(actual.metadata[key], typeof(System.Byte));
            }

            // now test by comparing their printed form
            CheckParse(p, "(a b c)", "(a b c)");
            CheckParse(p, " (   1.0 2.1   -3  #t   #f   ( ) a  b  c )  ", "(1 2.1 -3 #t #f () a b c)");
            CheckParse(p, "(a (b (c d)) e)", "(a (b (c d)) e)");
            CheckParse(p, "'(foo) '((a b) c) '()", "(quote (foo))", "(quote ((a b) c))", "(quote ())");
            CheckParse(p, "(a b ; c d)\n   e f)", "(a b e f)");

            CheckParse(p, "{}", "{}");
            CheckParse(p, "{0 1}", "{0 1}");
            CheckParse(p, "{0.1 1.2}", "{0.1 1.2}");
            CheckParse(p, """{"a" "b"}""", """{"a" "b"}""");
            // TODO: Split Print and Write, and special casing for quoting to match input
            CheckParse(p, "{'a 'b}", "{(quote a) (quote b)}");
            {
                var actual = ParseRaw(p, "{0 1 1 2 3 4}");
                var expected = new Val(new Dictionary<Val, Val>
                {
                    {0, 1},
                    {1, 2},
                    {3, 4},
                }.ToImmutableDictionary());
                Assert.Equal(expected, actual);
            }

            // now check backquotes
            CheckParse(p, "foo 'foo `foo `,foo", "foo", "(quote foo)", "(quote foo)", "foo");
            CheckParse(p, "`(foo)", "(list (quote foo))");
            CheckParse(p, "`(foo foo)", "(list (quote foo) (quote foo))");
            CheckParse(p, "`(,foo)", "(list foo)");
            CheckParse(p, "`(,@foo)", "(append foo)");
        }

        private Val ParseRaw(Parser parser, string input)
        {
            parser.AddString(input);

            List<Val> parsed = parser.ParseAll();
            Assert.Equal(1, parsed.Count);

            Val result = parsed[0];
            return result;
        }

        /// <summary> Test helper - takes parse results, converts them to the canonical string form, and compares to outputs </summary>
        private void CheckParse(Parser parser, string input, params string[] expecteds)
        {
            parser.AddString(input);

            List<Val> results = parser.ParseAll();
            Assert.Equal(expecteds.Length, results.Count);

            for (int i = 0; i < results.Count; i++)
            {
                string result = Val.Print(results[i]);
                string expected = expecteds[i];
                Assert.Equal(expected, result);
            }
        }

        /// <summary> Compiles some sample scripts and prints them out, without validation. </summary>
        [Fact]
        public void PrintSampleCompilations()
        {
            Context ctx = new(false, _logger);

            CompileAndPrint(ctx, "5");
            CompileAndPrint(ctx, "\"foo\"");
            CompileAndPrint(ctx, "#t");
            CompileAndPrint(ctx, "'foo");
            CompileAndPrint(ctx, "(begin 1)");
            CompileAndPrint(ctx, "(begin 1 2 3)");
            CompileAndPrint(ctx, "x");
            CompileAndPrint(ctx, "(set! x (begin 1 2 3))");
            CompileAndPrint(ctx, "(begin (set! x (begin 1 2 3)) x)");
            CompileAndPrint(ctx, "(if p x y)");
            CompileAndPrint(ctx, "(begin (if p x y) z)");
            CompileAndPrint(ctx, "(if 5 x y)");
            CompileAndPrint(ctx, "(if #f x y)");
            CompileAndPrint(ctx, "(if x y)");
            CompileAndPrint(ctx, "(if p x (begin 1 2 x))");
            CompileAndPrint(ctx, "(if (not p) x y)");
            CompileAndPrint(ctx, "(if (if a b c) x y)");
            CompileAndPrint(ctx, "(lambda () 5)");
            CompileAndPrint(ctx, "((lambda () 5))");
            CompileAndPrint(ctx, "(lambda (a) a)");
            CompileAndPrint(ctx, "(lambda (a) (lambda (b) a))");
            CompileAndPrint(ctx, "(set! x (lambda (a) a))");
            CompileAndPrint(ctx, "((lambda (a) a) 5)");
            CompileAndPrint(ctx, "((lambda (x) ((lambda (y z) (f x y z)) 3 x)) 4)");
            CompileAndPrint(ctx, "(if a b (f c))");
            CompileAndPrint(ctx, "(if* (+ 1 2) b)");
            CompileAndPrint(ctx, "(if* #f b)");
            CompileAndPrint(ctx, "(begin (- 2 3) (+ 2 3))");
            //			compileAndPrint(ctx, "(begin (set! sum (lambda (x) (if (<= x 0) 0 (sum (+ 1 (- x 1)))))) (sum 5))");

            //DumpCodeBlocks(ctx);
        }

        /// <summary> Compiles an s-expression and prints the resulting assembly </summary>
        private void CompileAndPrint(Context ctx, string input)
        {
            Log("COMPILE inputs: ", input);
            ctx.Parser.AddString(input);

            List<Val> parseds = ctx.Parser.ParseAll();
            foreach (Val parsed in parseds)
            {
                CompilationResults results = ctx.Compiler.Compile(parsed);
                Log(ctx.Code.DebugPrint(results));
            }
        }

        /// <summary> Front-to-back test of the virtual machine </summary>
        [Fact]
        public void TestVmNoCoreLib()
        {
            // first without the standard library
            Context ctx = new(false, _logger);

            // test reserved keywords
            CompileAndRun(ctx, "5", "5");
            CompileAndRun(ctx, "#t", "#t");
            CompileAndRun(ctx, "\"foo\"", "\"foo\"");
            CompileAndRun(ctx, "(begin 1 2 3)", "3");
            CompileAndRun(ctx, "xyz", "()");
            CompileAndRun(ctx, "xyz", "()");
            CompileAndRun(ctx, "(set! x 5)", "5");
            CompileAndRun(ctx, "(begin (set! x 2) x)", "2");
            CompileAndRun(ctx, "(begin (set! x #t) (if x 5 6))", "5");
            CompileAndRun(ctx, "(begin (set! x #f) (if x 5 6))", "6");
            CompileAndRun(ctx, "(begin (if* 5 6))", "5");
            CompileAndRun(ctx, "(begin (if* (if 5 #f) 6))", "6");
            CompileAndRun(ctx, "(begin (if* (+ 1 2) 4) 5)", "5");
            CompileAndRun(ctx, "(begin (if* (if 5 #f) 4) 5)", "5");
            CompileAndRun(ctx, "((lambda (a) a) 5)", "5");
            CompileAndRun(ctx, "((lambda (a . b) b) 5 6 7 8)", "(6 7 8)");
            CompileAndRun(ctx, "((lambda (a) (set! a 6) a) 1)", "6");
            CompileAndRun(ctx, "((lambda (x . rest) (if x 'foo rest)) #t 'a 'b 'c)", "foo");
            CompileAndRun(ctx, "((lambda (x . rest) (if x 'foo rest)) #f 'a 'b 'c)", "(a b c)");
            CompileAndRun(ctx, "(begin (set! x (lambda (a b c) (if a b c))) (x #t 5 6))", "5");
            CompileAndRun(ctx, "(begin (set! x 0) (while (< x 5) (set! x (+ x 1)) x))", "5");
            CompileAndRun(ctx, "(begin (set! x 0) (while (< x 5) (set! x (+ x 1))) x)", "5");

            //DumpCodeBlocks(ctx);
        }

        /// <summary> Front-to-back test of the virtual machine </summary>
        [Fact]
        public void TestVmPrimitives()
        {
            // first without the standard library
            Context ctx = new(false, _logger);

            // test primitives
            CompileAndRun(ctx, "(+ 1 2)", "3");
            CompileAndRun(ctx, "(+ (+ 1 2) 3)", "6");
            CompileAndRun(ctx, "(+ 1 2 3 4)", "10");
            CompileAndRun(ctx, "(* 1 2 3 4)", "24");
            CompileAndRun(ctx, "(= 1 1)", "#t");
            CompileAndRun(ctx, "(!= 1 1)", "#f");
            CompileAndRun(ctx, "(cons 1 2)", "(1 . 2)");
            CompileAndRun(ctx, "`(a 1)", "(a 1)");
            CompileAndRun(ctx, "(list)", "()");
            CompileAndRun(ctx, "(list 1)", "(1)");
            CompileAndRun(ctx, "(list 1 2)", "(1 2)");
            CompileAndRun(ctx, "(list 1 2 3)", "(1 2 3)");
            CompileAndRun(ctx, "(length '(a b c))", "3");
            CompileAndRun(ctx, "(append '(1 2) '(3 4) '() '(5))", "(1 2 3 4 5)");
            CompileAndRun(ctx, "(list (append '() '(3 4)) (append '(1 2) '()))", "((3 4) (1 2))");
            CompileAndRun(ctx, "(list #t (not #t) #f (not #f) 1 (not 1) 0 (not 0))", "(#t #f #f #t 1 #f 0 #f)");
            CompileAndRun(ctx, "(list (null? ()) (null? '(a)) (null? 0) (null? 1) (null? #f))", "(#t #f #f #f #f)");
            CompileAndRun(ctx, "(list (cons? ()) (cons? '(a)) (cons? 0) (cons? 1) (cons? #f))", "(#f #t #f #f #f)");
            CompileAndRun(ctx, "(list (atom? ()) (atom? '(a)) (atom? 0) (atom? 1) (atom? #f))", "(#t #f #t #t #t)");
            CompileAndRun(ctx, "(list (closure? ()) (closure? 0) (closure? 'list) (closure? list))", "(#f #f #f #t)");
            CompileAndRun(ctx, "(list (number? ()) (number? '(a)) (number? 0) (number? 1) (number? #f))", "(#f #f #t #t #f)");
            CompileAndRun(ctx, "(list (string? ()) (string? '(a)) (string? 0) (string? 1) (string? #f) (string? \"foo\"))", "(#f #f #f #f #f #t)");
            CompileAndRun(ctx, "(begin (set! x '(1 2 3 4 5)) (list (car x) (cadr x) (caddr x)))", "(1 2 3)");
            CompileAndRun(ctx, "(begin (set! x '(1 2 3 4 5)) (list (cdr x) (cddr x) (cdddr x)))", "((2 3 4 5) (3 4 5) (4 5))");
            CompileAndRun(ctx, "(nth '(1 2 3 4 5) 2)", "3");
            CompileAndRun(ctx, "(nth-tail '(1 2 3 4 5) 2)", "(4 5)");
            CompileAndRun(ctx, "(nth-cons '(1 2 3 4 5) 2)", "(3 4 5)");
            CompileAndRun(ctx, "(trace \"foo\" \"bar\")", "()"); // trace outputs text instead
            CompileAndRun(ctx, "(begin (set! first car) (first '(1 2 3)))", "1");
            CompileAndRun(ctx, "(if (< 0 1) 5)", "5");
            CompileAndRun(ctx, "(if (> 0 1) 5)", "()");

            // test quotes, macros, and eval
            CompileAndRun(ctx, "`((list 1 2) ,(list 1 2) ,@(list 1 2))", "((list 1 2) (1 2) 1 2)");
            CompileAndRun(ctx, "(begin (set! x 5) (set! y '(a b)) `(x ,x ,y ,@y))", "(x 5 (a b) a b)");
            CompileAndRun(ctx, "(begin (defmacro inc1 (x) `(+ ,x 1)) (inc1 2))", "3");
            CompileAndRun(ctx, "(begin (defmacro foo (op . rest) `(,op ,@(map number? rest))) (foo list 1 #f 'a))", "(#t #f #f)");
            CompileAndRun(ctx, "(begin (defmacro lettest (bindings . body) `((lambda ,(map car bindings) ,@body) ,@(map cadr bindings))) (lettest ((x 1) (y 2)) (+ x y)))", "3");
            CompileAndRun(ctx, "(begin (defmacro inc1 (x) `(+ ,x 1)) (inc1 (inc1 (inc1 1))))", "4");
            CompileAndRun(ctx, "(begin (defmacro add (x y) `(+ ,x ,y)) (mx1 '(add 1 (add 2 3))))", "(core:+ 1 (add 2 3))");
            CompileAndRun(ctx, "(eval '(+ 1 2 3))", "6");
            CompileAndRun(ctx, "(eval '(inc1 1))", "2");
        }

        [Fact]
        public void TestDotNetInterop()
        {
            // load the standard library so we get access to (let ...) and macros in general
            Context ctx = new(true, _logger);

            // test dot net interop

            // get-type
            CompileAndRun(ctx, "(get-type 0)", typeof(System.Int32).ToString());
            CompileAndRun(ctx, "(get-type 0.0)", typeof(System.Single).ToString());
            CompileAndRun(ctx, "(get-type \"\")", typeof(System.String).ToString());

            // constructors
            CompileAndRun(ctx, "(.new (.. 'System.Random))", "[Native System.Random System.Random]");
            CompileAndRun(ctx, "(.new (.. 'System.Random) 0)", "[Native System.Random System.Random]");
            CompileAndRun(ctx, "(.new 'System.Random)", "[Native System.Random System.Random]");
            CompileAndRun(ctx, "(.new 'System.Random 0)", "[Native System.Random System.Random]");

            // field/property lookup and function calls
            CompileAndRun(ctx, "(.. 'System.Random)", "[Native System.RuntimeType System.Random]");
            CompileAndRun(ctx, "(.. \"foobar\" 'ToUpper)", "\"FOOBAR\"");
            CompileAndRun(ctx, "(.. \"foobar\" 'ToUpper.Length)", "6");
            CompileAndRun(ctx, "(.. 'System.Int32.Parse \"123\")", "123");
            CompileAndRun(ctx, "(.. \"foobar\" 'ToUpper 'Substring 0 3 'IndexOf \"O\")", "1");

            CompileAndRun(ctx, "(.. 'Paremnet.Inner.TestInner)", "[Native System.RuntimeType Paremnet.Inner.TestInner]");
            CompileAndRun(ctx, "(.. 'Paremnet.Inner.TestInner.staticField)", "42");
            CompileAndRun(ctx, "(.. 'Paremnet.Inner.TestInner.StaticFn 10)", "52");
            CompileAndRun(ctx, "(.. (.new 'System.DateTime 1999 12 31) 'ToString)", "\"12/31/1999 12:00:00 AM\"");
            CompileAndRun(ctx, "(.. (.new 'System.DateTime 1999 12 31) 'ToString \"yyyy\")", "\"1999\"");

            // field/property setters
            CompileAndRun(ctx,
                "(let* ((test (.new 'Paremnet.TestClass))" +
                "       (a (.. test 'MyIntField))" +
                "       (b (.. test 'MyIntProperty))" +
                "       (c (.. test 'MyStringField))" +
                "       (d (.. test 'MyFn 10)))" +
                "  (list a b c d))",
                "(42 0 \"hello\" 52)");

            CompileAndRun(ctx,
                "(let* ((test (.new 'Paremnet.TestClass))" +
                "       (a (.. test 'MyIntProperty))" +
                "       (b (.! test 'MyIntProperty 42))" +
                "       (c (.. test 'MyIntProperty)))" +
                "  (list a b c))",
                "(0 42 42)");

            CompileAndRun(ctx,
                "(let* ((test (.new 'Paremnet.TestClass))" +
                "       (a (.. test 'MyIntField))" +
                "       (b (.! test 'MyIntField 43))" +
                "       (c (.. test 'MyIntGetter)))" +
                "  (list a b c))",
                "(42 43 43)");

            CompileAndRun(ctx,
                "(let* ((test (.new 'Paremnet.TestStruct))" +
                "       (a (.. test 'MyIntField))" +
                "       (b (.! test 'MyIntField 42))" +
                "       (c (.. test 'MyIntGetter)))" +
                "  (list a b c))",
                "(0 42 42)");

            // indexed getters/setters (e.g. on arrays or dictionaries)

            CompileAndRun(ctx,
                "(let* ((test (.new 'System.Collections.ArrayList 10)))" +
                "   (.. test 'Add 42)" +
                "   (.. test 'Item 0))",
                "42");

            CompileAndRun(ctx,
                "(let* ((test (.new 'System.Collections.ArrayList 10)))" +
                "   (.. test 'Add 42)" +
                "   (.! test 'Item 0 43)" +
                "   (.. test 'Item 0))",
                "43");


            //DumpCodeBlocks(ctx);
        }

        [Fact]
        public void TestPackages()
        {
            // without the standard library
            Context ctx = new(false, _logger);

            // test packages
            CompileAndRun(ctx, "(package-set \"foo\") (package-get)", "\"foo\"", "\"foo\"");
            CompileAndRun(ctx, "(package-set \"foo\") (package-import \"core\") (car '(1 2))", "\"foo\"", "()", "1");
            CompileAndRun(ctx, "(package-set nil) (set! x 5) (package-set \"foo\") (package-import \"core\") (set! x (+ 1 5)) (package-set nil) x", "()", "5", "\"foo\"", "()", "6", "()", "5");
            CompileAndRun(ctx, "(package-set \"foo\") (package-import \"core\") (set! first car) (first '(1 2))", "\"foo\"", "()", "[Closure/core:car]", "1");
            CompileAndRun(ctx, "(package-set \"a\") (package-export '(afoo)) (set! afoo 1) (package-set \"b\") (package-import \"a\") afoo", "\"a\"", "()", "1", "\"b\"", "()", "1");

            // test more integration
            CompileAndRun(ctx, "(package-set \"foo\")", "\"foo\"");
            CompileAndRun(ctx, "(begin (+ (+ 1 2) 3) 4)", "4");
            CompileAndRun(ctx, "(begin (set! incf (lambda (x) (+ x 1))) (incf (incf 5)))", "7");
            CompileAndRun(ctx, "(set! fact (lambda (x) (if (<= x 1) 1 (* x (fact (- x 1)))))) (fact 5)", "[Closure]", "120");
            CompileAndRun(ctx, "(set! fact-helper (lambda (x prod) (if (<= x 1) prod (fact-helper (- x 1) (* x prod))))) (set! fact (lambda (x) (fact-helper x 1))) (fact 5)", "[Closure]", "[Closure]", "120");
            CompileAndRun(ctx, "(begin (set! add +) (add 3 (add 2 1)))", "6");
            CompileAndRun(ctx, "(begin (set! kar car) (set! car cdr) (set! result (car '(1 2 3))) (set! car kar) result)", "(2 3)");
            CompileAndRun(ctx, "((lambda (x) (set! x 5) x) 6)", "5");

            //DumpCodeBlocks(ctx);
        }

        [Fact]
        public void TestStandardLibs()
        {
            // now initialize the standard library
            Context ctx = new(true, _logger);

            // test some basic functions
            CompileAndRun(ctx, "(map number? '(a 2 \"foo\"))", "(#f #t #f)");

            // test standard library
            CompileAndRun(ctx, "(package-set \"foo\")", "\"foo\"");
            CompileAndRun(ctx, "(mx1 '(let ((x 1)) x))", "((lambda (foo:x) foo:x) 1)");
            CompileAndRun(ctx, "(mx1 '(let ((x 1) (y 2)) (set! y 42) (+ x y)))", "((lambda (foo:x foo:y) (set! foo:y 42) (core:+ foo:x foo:y)) 1 2)");
            CompileAndRun(ctx, "(mx1 '(let* ((x 1) (y 2)) (+ x y)))", "(core:let ((foo:x 1)) (core:let* ((foo:y 2)) (core:+ foo:x foo:y)))");
            CompileAndRun(ctx, "(mx1 '(define x 5))", "(begin (set! foo:x 5) (quote foo:x))");
            CompileAndRun(ctx, "(mx1 '(define (x y) 5))", "(core:define foo:x (lambda (foo:y) 5))");
            CompileAndRun(ctx, "(list (gensym) (gensym) (gensym \"bar_\"))", "(foo:GENSYM-1 foo:GENSYM-2 foo:bar_3)");
            CompileAndRun(ctx, "(let ((x 1)) (+ x 1))", "2");
            CompileAndRun(ctx, "(let ((x 1) (y 2)) (set! y 42) (+ x y))", "43");
            CompileAndRun(ctx, "(let* ((x 1) (y x)) (+ x y))", "2");
            CompileAndRun(ctx, "(let ((x 1)) (let ((y x)) (+ x y)))", "2");
            CompileAndRun(ctx, "(letrec ((x (lambda () y)) (y 1)) (x))", "1");
            CompileAndRun(ctx, "(begin (let ((x 0)) (define (set v) (set! x v)) (define (get) x)) (set 5) (get))", "5");
            CompileAndRun(ctx, "(define x 5) x", "foo:x", "5");
            CompileAndRun(ctx, "(define (x y) y) (x 5)", "foo:x", "5");
            CompileAndRun(ctx, "(cons (first '(1 2 3)) (rest '(1 2 3)))", "(1 2 3)");
            CompileAndRun(ctx, "(list (and 1) (and 1 2) (and 1 2 3) (and 1 #f 2 3))", "(1 2 3 #f)");
            CompileAndRun(ctx, "(list (or 1) (or 2 1) (or (< 1 0) (< 2 0) 3) (or (< 1 0) (< 2 0)))", "(1 2 3 #f)");
            CompileAndRun(ctx, "(cond ((= 1 2) 2) ((= 1 4) 4) 0)", "0");
            CompileAndRun(ctx, "(cond ((= 2 2) 2) ((= 1 4) 4) 0)", "2");
            CompileAndRun(ctx, "(cond ((= 1 2) 2) ((= 4 4) 4) 0)", "4");
            CompileAndRun(ctx, "(case (+ 1 2) (2 #f) (3 #t) 'error)", "#t");
            CompileAndRun(ctx, "(let ((r '())) (for (i 0 (< i 3) (+ i 1)) (set! r (cons i r))) r)", "(2 1 0)");
            CompileAndRun(ctx, "(let ((r '())) (dotimes (i 3) (set! r (cons i r))) r)", "(2 1 0)");
            CompileAndRun(ctx, "(apply + '(1 2))", "3");
            CompileAndRun(ctx, "(apply cons '(1 2))", "(1 . 2)");
            CompileAndRun(ctx, "(fold-left cons '() '(1 2))", "((() . 1) . 2)");
            CompileAndRun(ctx, "(fold-right cons '() '(1 2))", "(1 2)");
            CompileAndRun(ctx, "(zip '(1 2) '(11 12))", "((1 11) (2 12))");
            CompileAndRun(ctx, "(reverse '(1 2 3))", "(3 2 1)");
            CompileAndRun(ctx, "(reverse '(1 (2 3 4) 5)", "(5 (2 3 4) 1)");
            CompileAndRun(ctx, "(index-of 'a '(a b c))", "0");
            CompileAndRun(ctx, "(index-of 'c '(a b c))", "2");
            CompileAndRun(ctx, "(index-of 1 '(a b c))", "()");
            CompileAndRun(ctx, "(begin (set! x '(1 2 3 4 5)) (list (first x) (second x) (third x)))", "(1 2 3)");
            CompileAndRun(ctx, "(begin (set! x '(1 2 3 4 5)) (list (after-first x) (after-second x) (after-third x)))", "((2 3 4 5) (3 4 5) (4 5))");
            CompileAndRun(ctx, "(set! add (let ((sum 0)) (lambda (delta) (set! sum (+ sum delta)) sum))) (add 0) (add 100) (add 0)", "[Closure]", "0", "100", "100");
            CompileAndRun(ctx, "((chain-list (list cdr cdr car)) '(1 2 3 4))", "3");
            CompileAndRun(ctx, "((chain cdr cdr car) '(1 2 3 4))", "3");

            CompileAndRun(ctx, "(vector? (make-vector 3))", "#t");
            CompileAndRun(ctx, "(vector? 1)", "#f");
            CompileAndRun(ctx, "(make-vector 3)", "[Vector () () ()]");
            CompileAndRun(ctx, "(make-vector 3 0)", "[Vector 0 0 0]");
            CompileAndRun(ctx, "(make-vector '(1 2))", "[Vector 1 2]");
            CompileAndRun(ctx, "(vector-length (make-vector '(1 2)))", "2");
            CompileAndRun(ctx, "(vector-get (make-vector '(1 2)) 0)", "1");
            CompileAndRun(ctx, "(let ((v (make-vector '(1 2)))) (vector-set! v 0 3) v)", "[Vector 3 2]");

            //DumpCodeBlocks(ctx);
        }

        [Fact]
        public void TestRecords()
        {
            Context ctx = new(true, _logger);

            // make a new point record with fields x and y (x has a getter and setter, y has a getter only)
            CompileAndRun(ctx, "(define-record-type point (make-point x y) point? (x getx setx!) (y gety))", "()");

            // test the new record
            CompileAndRun(ctx, "(define p (make-point 1 2))", "user:p");

            CompileAndRun(ctx, "(point? p)", "#t");
            CompileAndRun(ctx, "(point? 1)", "#f");
            CompileAndRun(ctx, "(point? '(a b))", "#f");

            CompileAndRun(ctx, "p", "[Vector [Closure] 1 2]");
            CompileAndRun(ctx, "(getx p)", "1");
            CompileAndRun(ctx, "(setx! p 42)", "42");
            CompileAndRun(ctx, "p", "[Vector [Closure] 42 2]");
            CompileAndRun(ctx, "(gety p)", "2");
        }


        public void PrintAllStandardLibraries()
        {
            Context ctx = new(true, _logger);
            DumpCodeBlocks(ctx);
        }

        /// <summary> Compiles an s-expression, runs the resulting code, and checks the output against the expected value </summary>
        private void CompileAndRun(Context ctx, string input, params string[] expecteds)
        {
            ctx.Parser.AddString(input);
            Log("\n\n-------------------------------------------------------------------------");
            Log("\n\nCOMPILE AND RUN inputs: ", input);

            for (int i = 0, count = expecteds.Length; i < count; i++)
            {
                string expected = expecteds[i];

                Val result = ctx.Parser.ParseNext();
                Log("Parsed: ", result);

                CompilationResults comp = ctx.Compiler.Compile(result);
                Log("Compiled:");
                Log(ctx.Code.DebugPrint(comp));

                Log("Running...");
                Val output = ctx.Vm.Execute(comp.Closure);
                string formatted = Val.Print(output);
                Assert.Equal(new Val(expected), new Val(formatted));
            }
        }
    }
}
