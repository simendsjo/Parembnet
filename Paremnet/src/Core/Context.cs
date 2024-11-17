using Paremnet.Data;
using Paremnet.Libs;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Paremnet.Core;

/// <summary>
/// Definition of a logger that will capture debug info about compilation and execution.
/// </summary>
public interface ILogger
{
    /// <summary> When true, we will log expression parsing details </summary>
    bool EnableParsingLogging { get; }

    /// <summary> When true, we will log every executed instruction </summary>
    bool EnableInstructionLogging { get; }

    /// <summary> When true, we will log stack state at the end of each executed instruction </summary>
    bool EnableStackLogging { get; }

    /// <summary> Type signature for the actual debug logging function </summary>
    void Log(params object[] args);
}

/// <summary>
/// Binds together an instance of a compiler, parser, and executor.
/// </summary>
public class Context
{
    public readonly Code Code;
    public readonly Packages Packages;
    public readonly Parser Parser;
    public readonly Compiler Compiler;
    public readonly Machine Vm;

    public Context(bool loadLibraries = true, ILogger logger = null)
    {
        Code = new Code();
        Packages = new Packages();
        Parser = new Parser(Packages, logger);
        Compiler = new Compiler(this);
        Vm = new Machine(this, logger);

        Primitives.InitializeCorePackage(this, Packages.Core);

        if (loadLibraries)
        {
            Libraries.LoadStandardLibraries(this);
        }
    }

    /// <summary> Stores the result of compiling a given code block and executing it </summary>
    public struct CompileAndExecuteResult
    {
        public string Input;
        public CompilationResults Comp;
        public Val Output;
        public TimeSpan Exectime;
    }

    /// <summary> Convenience wrapper that processes the input as a string, and returns an array of results. </summary>
    public List<CompileAndExecuteResult> CompileAndExecute(string input)
    {

        List<CompileAndExecuteResult> outputs = new();

        Parser.AddString(input);
        List<Val> parseResults = Parser.ParseAll();

        foreach (Val result in parseResults)
        {
            CompilationResults cr = Compiler.Compile(result);

            Stopwatch s = Stopwatch.StartNew();
            Val output = Vm.Execute(cr.Closure);
            s.Stop();

            outputs.Add(new CompileAndExecuteResult
            {
                Input = input,
                Comp = cr,
                Output = output,
                Exectime = s.Elapsed
            });
        }

        return outputs;
    }
}
