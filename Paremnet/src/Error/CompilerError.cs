using System;

namespace Paremnet.Error;

/// <summary>
/// Class for errors thrown during the compilation phase 
/// </summary>
public class CompilerError(string message) : Exception(message);