using System;

namespace Paremnet.Error;

/// <summary>
/// Class for errors related to the language engine, not specific to a particular pass.
/// </summary>
public class LanguageError(string message) : Exception(message);