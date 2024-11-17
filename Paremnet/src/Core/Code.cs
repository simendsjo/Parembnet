using Paremnet.Data;
using Paremnet.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Paremnet.Core;

/// <summary> Opaque handle to a code block entry in storage </summary>
public struct CodeHandle(int index) : IEquatable<CodeHandle>
{
    public int Index = index;

    public bool IsValid => Index > 0; // index at 0 is always null

    public static bool Equals(CodeHandle a, CodeHandle b) => a.Index == b.Index;
    public static bool operator ==(CodeHandle a, CodeHandle b) => Equals(a, b);
    public static bool operator !=(CodeHandle a, CodeHandle b) => !Equals(a, b);

    public bool Equals(CodeHandle other) => Equals(this, other);

    public override bool Equals(object obj) => obj is CodeHandle handle && Equals(this, handle);
    public override int GetHashCode() => Index;
}

/// <summary> Code block stores a collection of instructions and some additional debugging data </summary>
public class CodeBlock(CodeHandle handle, List<Instruction> instructions, string debug)
{
    public CodeHandle Handle = handle;
    public readonly List<Instruction> Instructions = instructions;
    public readonly string Debug = debug;
}

/// <summary>
/// Storage for compiled code blocks, putting them in one place for easier debugging
/// </summary>
public class Code
{
    // block storage as a list to ensure fast lookup
    private readonly List<CodeBlock> _blocks = [null]; // make sure index starts at 1

    public CodeHandle LastHandle => new(_blocks.Count - 1);

    /// <summary> Registers a new code block and returns its handle </summary>
    public CodeHandle AddBlock(List<Instruction> instructions, string debug)
    {
        CodeHandle handle = new() { Index = _blocks.Count };
        _blocks.Add(new CodeBlock(handle, instructions, debug));
        return handle;
    }

    /// <summary> Deregisters the specified code block and replaces it with null. </summary>
    public void RemoveBlock(CodeHandle handle)
    {
        if (handle.Index <= 0 || handle.Index >= _blocks.Count)
        {
            throw new LanguageError("Invalid code block handle!");
        }

        _blocks[handle.Index] = null; // note we just leave a hole, so we don't renumber other ones
    }

    /// <summary> Retrieves a code block registered for a given handle </summary>
    public CodeBlock Get(CodeHandle handle)
    {
        if (handle.Index <= 0 || handle.Index >= _blocks.Count)
        {
            throw new LanguageError("Invalid code block handle!");
        }

        return _blocks[handle.Index];
    }

    /// <summary> Returns an iterator over all code blocks. Some may be null! </summary>
    public IEnumerable<CodeBlock> GetAll() => _blocks;

    /// <summary> Converts a compilation result to a string </summary>
    public string DebugPrint(CompilationResults comp, int indentLevel = 1) =>
        string.Join("\n", comp.Recents.Select(h => DebugPrint(h, indentLevel)));

    /// <summary> Converts a set of instructions to a string </summary>
    public string DebugPrint(Closure cl, int indentLevel = 1) =>
        DebugPrint(cl.Code, indentLevel);

    /// <summary> Converts a set of instructions to a string </summary>
    private string DebugPrint(CodeHandle handle, int indentLevel)
    {
        CodeBlock block = Get(handle);
        StringBuilder sb = new();

        sb.Append('\t', indentLevel);
        //sb.AppendLine($"CODE BLOCK # {block.handle.index} ; {block.debug}");
        sb.AppendLine($"CODE BLOCK ; {block.Debug}");

        for (int i = 0, count = block.Instructions.Count; i < count; i++)
        {
            Instruction instruction = block.Instructions[i];

            // tab out and print current instruction
            int tabs = indentLevel + (instruction.Type == Opcode.Label ? -1 : 0);
            sb.Append('\t', tabs);
            sb.Append(i);
            sb.Append('\t');
            sb.AppendLine(instruction.DebugPrint());

            //if (instruction.type == Opcode.MAKE_CLOSURE) {
            //    // if function, recurse
            //    Closure closure = instruction.first.AsClosure;
            //    sb.Append(DebugPrint(closure, indentLevel + 1));
            //}
        }
        return sb.ToString();
    }

    /// <summary> Converts all sets of instructions to a string </summary>
    public string DebugPrintAll()
    {
        StringBuilder sb = new();
        sb.AppendLine("\n\n*** ALL CODE BLOCKS\n");

        for (int i = 0; i < _blocks.Count; i++)
        {
            CodeBlock block = _blocks[i];
            if (block != null)
            {
                sb.AppendLine(DebugPrint(block.Handle, 1));
            }
        }

        sb.AppendLine("*** END OF ALL CODE BLOCKS\n");
        return sb.ToString();
    }
}
