using Paremnet.Core;
using Paremnet.Data;
using System.Collections.Generic;
using System.IO;

namespace Paremnet.Libs;

/// <summary>
/// Manages standard libraries
/// </summary>
public class Libraries
{
    /// <summary> All libraries as a list </summary>
    private static List<byte[]> GetAllBuiltInLibraries() =>
        [Resources.Core, Resources.Record, Resources.User];

    /// <summary> Loads all standard libraries into an initialized machine instance </summary>
    public static void LoadStandardLibraries(Context ctx)
    {
        List<byte[]> allLibs = GetAllBuiltInLibraries();
        foreach (byte[] libBytes in allLibs)
        {
            using MemoryStream stream = new(libBytes);
            using StreamReader reader = new(stream);
            string libText = reader.ReadToEnd();
            LoadLibrary(ctx, libText);
        }
    }

    /// <summary> Loads a single string into the execution context </summary>
    private static void LoadLibrary(Context ctx, string lib)
    {
        ctx.Parser.AddString(lib);

        while (true)
        {
            Val result = ctx.Parser.ParseNext();
            if (Val.Equals(Parser.Eof, result))
            {
                break;
            }

            Closure cl = ctx.Compiler.Compile(result).Closure;
            Val _ = ctx.Vm.Execute(cl);
            // and we drop the output on the floor... for now... :)
        }
    }

}
