using Paremnet.Error;
using System.Linq;

namespace Paremnet.Data;

/// <summary>
/// Stores variable lookup data: target frame index relative to current frame
/// (0 = this frame, 1 = the previous one, etc) and then symbol index
/// in the target frame, similarly zero-indexed.
/// </summary>
public struct VarPos
{
    public static readonly VarPos Invalid = new(-1, -1);

    public readonly int FrameIndex, SymbolIndex;

    public VarPos(Val frameIndex, Val symbolIndex)
    {
        this.FrameIndex = frameIndex.AsInt;
        this.SymbolIndex = symbolIndex.AsInt;
    }

    public VarPos(int frameIndex, int symbolIndex)
    {
        this.FrameIndex = frameIndex;
        this.SymbolIndex = symbolIndex;
    }

    public bool IsValid => FrameIndex >= 0 && SymbolIndex >= 0;
    public bool IsNotValid => !IsValid;
}

/// <summary>
/// An Environment instance binds variables to their values.
/// Variable names are for compilation only - they're not used at runtime
/// (except for debugging help).
/// </summary>
public class Environment(int count, Environment parent)
{
    /// <summary> Parent environment </summary>
    public readonly Environment Parent = parent;

    /// <summary> Symbols defined in this environment </summary>
    private readonly Symbol[] _symbols = new Symbol[count];

    /// <summary> Values defined for each symbol </summary>
    private readonly Val[] _values = new Val[count];

    /// <summary> Creates a new environment from a cons'd list of arguments </summary>
    public static Environment Make(Cons args, Environment parent)
    {
        int count = Cons.Length(args);
        Environment env = new(count, parent);

        for (int i = 0; i < count; i++)
        {
            env.SetSymbol(i, args.First.AsSymbol);
            env.SetValue(i, Val.NIL);
            args = args.Rest.AsConsOrNull;
        }

        return env;
    }

    /// <summary> Retrieves symbol at the given index </summary>
    public Symbol GetSymbol(int symbolIndex) => _symbols[symbolIndex];

    /// <summary> Sets symbol at the given index </summary>
    private void SetSymbol(int symbolIndex, Symbol symbol) => _symbols[symbolIndex] = symbol;

    /// <summary> Retrieves the index of the given symbol </summary>
    private int IndexOfSymbol(Symbol symbol)
    {
        for (int i = 0, count = _symbols.Length; i < count; i++)
        {
            if (_symbols[i] == symbol) { return i; }
        }
        return -1;
    }

    /// <summary> Retrieves value at the given index </summary>
    public Val GetValue(int symbolIndex) => _values[symbolIndex];

    /// <summary> Sets value at the given index </summary>
    public void SetValue(int symbolIndex, Val value) => _values[symbolIndex] = value;

    /// <summary> Returns the number of slots defined in this environment </summary>
    public int Length => _symbols.Length;

    /// <summary> 
    /// Returns coordinates of a symbol, relative to the given environment, or null if not present.
    /// First element of the vector is the index of the environment, in the chain,
    /// and the second element is the index of the variable itself. 
    /// </summary>
    public static VarPos GetVariable(Symbol symbol, Environment frame)
    {
        int frameIndex = 0;
        while (frame != null)
        {
            int symbolIndex = frame.IndexOfSymbol(symbol);
            if (symbolIndex >= 0)
            {
                return new VarPos(frameIndex, symbolIndex);
            }
            else
            {
                frame = frame.Parent;
                frameIndex++;
            }
        }
        return VarPos.Invalid;
    }

    /// <summary> Retrieves the symbol at the given coordinates, relative to the current environment. </summary>
    public static Symbol GetSymbolAt(VarPos varref, Environment frame)
        => GetFrame(varref.FrameIndex, frame).GetSymbol(varref.SymbolIndex);

    /// <summary> Sets the symbol at the given coordinates, relative to the current environment. </summary>
    public static void SetSymbolAt(VarPos varref, Symbol symbol, Environment frame)
        => GetFrame(varref.FrameIndex, frame).SetSymbol(varref.SymbolIndex, symbol);

    /// <summary> Retrieves the value at the given coordinates, relative to the current environment. </summary>
    public static Val GetValueAt(VarPos varref, Environment frame)
        => GetFrame(varref.FrameIndex, frame).GetValue(varref.SymbolIndex);

    /// <summary> Sets the value at the given coordinates, relative to the current environment. </summary>
    public static void SetValueAt(VarPos varref, Val value, Environment frame)
        => GetFrame(varref.FrameIndex, frame).SetValue(varref.SymbolIndex, value);

    /// <summary> Returns the specified frame, relative to the current environment </summary>
    private static Environment GetFrame(int frameIndex, Environment frame)
    {
        for (int i = 0; i < frameIndex; i++)
        {
            frame = frame.Parent;
            if (frame == null)
            {
                throw new LanguageError("Invalid frame coordinates detected");
            }
        }
        return frame;
    }

    public string DebugPrintSymbols() => "(" + string.Join(" ", _symbols.Select(s => Val.DebugPrint(s))) + ")";
}