using System;

namespace Paremnet.Error;

/// <summary>
/// Class for errors encountered during the parsing phase
/// </summary>
public class ParserError(string message) : Exception(message);
