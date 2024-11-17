using Paremnet.Core;
using Paremnet.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Paremnet
{
    internal class Repl
    {
        private static bool
            _runRepl = true,
            _showPrompt = true,
            _logCompilation = false,
            _logExecution = false,
            _timeNextExecution = false;

        private class Command(string name, string description, Action action)
        {
            public string Name = name, Description = description;
            public Action Action = action;

            public string Message => $"{Name} - {Description}";
        }

        private class Logger : ILogger
        {
            public bool EnableParsingLogging => _logExecution;
            public bool EnableInstructionLogging => _logExecution;
            public bool EnableStackLogging => _logExecution;

            public void Log(params object[] args)
            {
                IEnumerable<string> strings = args.Select(obj => (obj == null) ? "null" : obj.ToString());
                string message = string.Join(" ", strings);
                Console.WriteLine(message);
            }
        }

        private static readonly List<Command> Commands =
        [
            new Command(",exit", "Quits the REPL", () => _runRepl = false),
            new Command(",help", "Shows this help menu",
                () => Console.WriteLine("Valid repl commands:\n" +
                                        string.Join("\n", Commands.Select(p => p.Message)))),


            new Command(",logcomp", "Toggles logging of bytecode compilation",
                () =>
                {
                    _logCompilation = !_logCompilation;
                    Console.WriteLine("Logging compilation: " + _logCompilation);
                }),

            new Command(",logexec", "Toggles logging of bytecode execution",
                () =>
                {
                    _logExecution = !_logExecution;
                    Console.WriteLine("Logging execution: " + _logExecution);
                }),

            new Command(",time", "Type ',time (expression ...)' to log and print execution time of that expression",
                () => _timeNextExecution = true)
        ];

        public static void Run()
        {

            Context ctx = new(logger: new Logger());
            Console.WriteLine(GetInfo(ctx));

            IEnumerable<Val> selfTest = ctx.CompileAndExecute("(+ 1 2)").Select(r => r.Output);
            Console.WriteLine();
            Console.WriteLine("SELF TEST: (+ 1 2) => " + string.Join(" ", selfTest));
            Console.WriteLine("Type ,help for list of repl commands or ,exit to quit.\n");

            while (_runRepl)
            {
                if (_showPrompt)
                {
                    Console.Write("> ");
                    _showPrompt = false;
                }

                string line = Console.ReadLine();
                Command cmd = Commands.Find(c => line.StartsWith(c.Name));

                try
                {
                    if (cmd != null)
                    {
                        line = line.Remove(0, cmd.Name.Length).TrimStart();
                        cmd.Action.Invoke();
                    }

                    Stopwatch s = _timeNextExecution ? Stopwatch.StartNew() : null;
                    List<Context.CompileAndExecuteResult> results = ctx.CompileAndExecute(line);
                    LogCompilation(ctx, results);
                    LogExecutionTime(results, s);

                    results.ForEach(entry => Console.WriteLine(Val.Print(entry.Output)));

                    _showPrompt = cmd != null || results.Count > 0;

                }
                catch (Error.LanguageError e)
                {
                    Console.Error.WriteLine("ERROR: " + e.Message);
                    _showPrompt = true;

                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            }

        }

        private static void LogExecutionTime(List<Context.CompileAndExecuteResult> results, Stopwatch s)
        {
            if (s == null) { return; }

            _timeNextExecution = false;

            foreach (Context.CompileAndExecuteResult result in results)
            {
                Console.WriteLine($"Execution took {result.Exectime.TotalSeconds} seconds for: {result.Input}");
            }
        }

        private static void LogCompilation(Context ctx, List<Context.CompileAndExecuteResult> results)
        {
            if (!_logCompilation) { return; }

            results.ForEach(result => Console.WriteLine(ctx.Code.DebugPrint(result.Comp)));
        }

        private static string GetInfo(Context ctx)
        {
            string manifestLocation = ctx.GetType().Assembly.Location;
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(manifestLocation);
            return $"Paremnet.REPL. {info.LegalCopyright}. Version {info.ProductVersion}.";
        }

        private static void Main(string[] _) => Run();
    }
}

#pragma warning disable CA2211 // Non-constant fields should not be visible
namespace Paremnet
{
    /// <summary>
    /// This class is purely for testing .net interop during development, and may be removed later
    /// </summary>
    public class TestClass
    {
        public int MyIntField = 42;
        public int MyIntGetter => MyIntField;
        public int MyIntProperty { get; set; }

        public string MyStringField = "hello";
    }

    namespace Inner
    {
        /// <summary>
        /// This class is purely for testing .net interop during development, and may be removed later
        /// </summary>
        public class TestInner
        {
            public static int staticField = 42;

            public static int StaticProperty
            {
                get => staticField;
                set => staticField = value;
            }

            public static int StaticFn(int x) => x + 42;
        }
    }
}
#pragma warning restore CA2211 // Non-constant fields should not be visible
