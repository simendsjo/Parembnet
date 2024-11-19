using Paremnet.Data;
using Paremnet.Error;
using Paremnet.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Paremnet.Core;

/// <summary>
/// Parser reads strings, and spits out s-expressions
/// </summary>
public class Parser(Packages packages, ILogger logger)
{
    /// <summary> Full list of reserved keywords - no symbol can be named as one of these </summary>
    public static readonly List<string> Reserved
        = ["quote", "begin", "set!", "if", "if*", "while", "lambda", "defmacro", "."];

    /// <summary> Special "end of stream" constant </summary>
    public static readonly Val Eof = new("!eof");

    /// <summary> Internal stream </summary>
    private readonly InputStream _stream = new();

    /// <summary> Reference to the global packages manager </summary>
    private readonly Packages _packages = packages ?? throw new ParserError("Parser requires a valid packages structure during initialization");

    /// <summary> Global unnamed package, used for symbols like "quote" (convenience reference) </summary>
    private readonly Package _global = packages.Global;

    /// <summary> Optional logger callback </summary>
    private readonly ILogger _logger = logger;

    /// <summary> Adds a new string to the parse buffer </summary>
    public void AddString(string str) => _stream.Add(str);

    /// <summary> Parses and returns all the elements it can from the stream </summary>
    public List<Val> ParseAll()
    {
        List<Val> results = [];
        Val result = ParseNext();
        while (!Val.Equals(result, Eof))
        {
            results.Add(result);
            result = ParseNext();
        }
        return results;
    }

    /// <summary>
    /// Parses the next element out of the stream (just one, if the stream contains more).
    /// Returns EOF and restores the stream, if no full element has been found.
    /// </summary>
    public Val ParseNext()
    {
        _stream.Save();
        Val result = Parse(_stream);
        if (Val.Equals(result, Eof))
        {
            _stream.Restore();
            return Eof;
        }

        if (_logger.EnableParsingLogging)
        {
            _logger.Log("ParseNext ==> ", Val.DebugPrint(result));
        }

        return result;
    }

    /// <summary>
    /// Parses an expression out of the stream.
    /// If the stream contains more than one expression, stops after the first one.
    /// If the stream did not contain a full expression, returns EOF.
    ///
    /// If backquote is true, we are recursively parsing inside a backquote expression
    /// which changes some of the parse behavior.
    /// </summary>
    private Val Parse(InputStream stream, bool backquote = false)
    {
        // pull out the first character, we'll dispatch on it
        if (stream.IsEmpty)
        {
            return Eof;
        }

        // remove leading whitespace
        ConsumeWhitespace(stream);

        // check for special forms
        Val result;
        char c = stream.Peek();
        switch (c)
        {
            case ';':
                ConsumeToEndOfLine(stream);
                result = Parse(stream, backquote);
                break;
            case '(':
                // this function will take care of the list, including the closing paren
                result = ParseList(stream, backquote);
                break;
            case ')':
                // well that was unexpected
                throw new ParserError("Unexpected closed parenthesis!");
            case '{':
                // this function will take care of the map, including the closing bracket
                result = ParseMap(stream, backquote);
                break;
            case '}':
                // well that was unexpected
                throw new ParserError("Unexpected closed curly bracket!");
            case '\"':
                // this function will take care of the string, including the closing quote
                result = ParseString(stream);
                break;
            case '\'':
                // 'foo => (quote foo)
                {
                    stream.Read();
                    Val body = Parse(stream, backquote);
                    result = Cons.MakeList(_global.Intern("quote"), body);
                }
                break;
            case '`':
                // `foo => (` foo) => converted value
                {
                    stream.Read();
                    Val body = Parse(stream, true);
                    Cons aslist = Cons.MakeList(_global.Intern("`"), body);
                    result = ConvertBackquote(aslist);
                }
                break;
            case ',':
                // ,foo => (, foo)
                // except that
                // ,@foo => (,@ foo)
                {
                    stream.Read();
                    if (!backquote)
                    {
                        throw new ParserError("Unexpected unquote!");
                    }
                    bool atomicUnquote = true;
                    if (stream.Peek() == '@')
                    {
                        stream.Read();
                        atomicUnquote = false;
                    }
                    Val body = Parse(stream, false);
                    result = Cons.MakeList(_global.Intern(atomicUnquote ? "," : ",@"), body);
                }
                break;
            default:
                // just a value. pick how to parse
                result = ParseAtom(stream, backquote);
                break;
        }

        // consume trailing whitespace
        ConsumeWhitespace(stream);

        return result;
    }

    /// <summary> Is this one of the standard whitespace characters? </summary>
    private static bool IsWhitespace(char ch) => char.IsWhiteSpace(ch);

    /// <summary> Eats up whitespace, nom nom </summary>
    private static void ConsumeWhitespace(InputStream stream)
    {
        while (IsWhitespace(stream.Peek())) { stream.Read(); }
    }

    /// <summary> Eats up everything till end of line </summary>
    private static void ConsumeToEndOfLine(InputStream stream)
    {
        char c = stream.Peek();
        while (c != '\n' && c != '\r')
        {
            stream.Read();
            c = stream.Peek();
        }
    }

    private readonly List<char> _specialElements = ['(', ')', '\"', '\'', '`', '{', '}'];

    /// <summary> Special elements are like whitespace - they interrupt tokenizing </summary>
    private bool IsSpecialElement(char elt, bool insideBackquote) => _specialElements.Contains(elt) || (insideBackquote && elt == ',');


    /// <summary>
    /// Parses a single element (token), based on following rules:
    ///   - if it's #t, it will be converted to a boolean true
    ///   - otherwise if it starts with #, it will be converted to a boolean false
    ///   - otherwise if it starts with +, -, or a digit, it will be converted to a number
    ///     (int or float, assuming parsing validation passes)
    ///   - otherwise it will be returned as a symbol
    /// </summary>
    private Val ParseAtom(InputStream stream, bool backquote)
    {

        // tokenizer loop
        StringBuilder sb = new();
        char ch;
        while ((ch = stream.Peek()) != (char)0)
        {
            if (IsWhitespace(ch) || IsSpecialElement(ch, backquote))
            {
                break; // we're done here, don't touch the special character
            }
            sb.Append(ch);
            stream.Read(); // consume and advance to the next one
        }

        // did we fail?
        if (sb.Length == 0)
        {
            return Eof;
        }

        string str = sb.ToString();

        // #t => true, #(anything) => false
        char c0 = str[0];
        if (c0 == '#')
        {
            if (str.Length == 2 && (str[1] == 't' || str[1] == 'T'))
            {
                return new Val(true);
            }
            else
            {
                return new Val(false);
            }
        }

        // parse if it starts with -, +, or a digit, but fall back if it causes a parse error
        if (c0 == '-' || c0 == '+' || char.IsDigit(c0))
        {
            Val num = ParseNumber(str);
            if (num.IsNotNil)
            {
                return num;
            }
        }

        // parse as symbol
        return ParseSymbol(str);
    }

    /// <summary> Parses as a number, an int or a float (the latter if there is a period present) </summary>
    private static Val ParseNumber(string val)
    {
        try
        {
            return val.Contains('.')
                ? new Val(float.Parse(val, CultureInfo.InvariantCulture))
                : new Val(int.Parse(val, CultureInfo.InvariantCulture));
        }
        catch (Exception)
        {
            return Val.NIL;
        }
    }

    /// <summary> Parses as a symbol, taking into account optional package prefix </summary>
    private Val ParseSymbol(string name)
    {
        // if this is a reserved keyword, always using global namespace
        if (Reserved.Contains(name)) { return new Val(_global.Intern(name)); }

        // figure out the package. default to current package.
        Package p = _packages.Current;

        // reference to a non-current package - let's look it up there
        int colon = name.IndexOf(':');
        if (colon >= 0)
        {
            string pkgname = name[..colon];
            p = _packages.Intern(pkgname);  // we have a specific package name, look there instead
            if (p == null)
            {
                throw new ParserError("Unknown package: " + pkgname);
            }
            name = name[(colon + 1)..];
        }

        // do we have the symbol anywhere in that package or its imports?
        Symbol result = p.Find(name, true);
        if (result != null)
        {
            return new Val(result);
        }

        // never seen it before - intern it!
        return new Val(p.Intern(name));
    }

    /// <summary>
    /// Starting with an opening double-quote, it will consume everything up to and including closing double quote.
    /// Any characters preceded by backslash will be escaped.
    /// </summary>
    private static Val ParseString(InputStream stream)
    {

        StringBuilder sb = new();

        stream.Read(); // consume the opening quote

        while (true)
        {
            char ch = stream.Read();
            if (ch == (char)0) { throw new ParserError($"string not properly terminated: {sb}"); }

            // if we've consumed the closing double-quote, we're done.
            if (ch == '\"') { break; }

            // we got the escape - use the next character instead, whatever it is
            if (ch == '\\') { ch = stream.Read(); }

            sb.Append(ch);
        }

        return new Val(sb.ToString());
    }

    /// <summary>
    /// Starting with an open paren, recursively parse everything up to the matching closing paren,
    /// and then return it as a sequence of conses.
    /// </summary>
    private Val ParseList(InputStream stream, bool backquote)
    {

        List<Val> results = [];
        stream.Read(); // consume opening paren
        ConsumeWhitespace(stream);

        char ch;
        while ((ch = stream.Peek()) != ')' && ch != (char)0)
        {
            Val val = Parse(stream, backquote);
            results.Add(val);
        }

        stream.Read(); // consume the closing paren
        ConsumeWhitespace(stream);

        return Cons.MakeList(results);
    }

    private Val ParseMap(InputStream stream, bool backquote)
    {
        var result = ImmutableDictionary.CreateBuilder<Val, Val>();
        stream.Read(); // consume opening paren
        ConsumeWhitespace(stream);

        char ch;
        while ((ch = stream.Peek()) != '}' && ch != (char)0)
        {
            Val key = Parse(stream, backquote);
            Val val = Parse(stream, backquote);
            result.Add(key, val);
        }

        stream.Read(); // consume the closing bracket
        ConsumeWhitespace(stream);

        return result.ToImmutable();
    }

    /// <summary>
    /// Converts a backquote expression according to the following rules:
    ///
    /// <pre>
    /// (` e) where e is atomic => (quote e)
    /// (` (, e)) => e
    /// (` (a ...)) => (append [a] ...) but transforming elements:
    ///   [(, a)] => (list a)
    ///   [(,@ a)] => a
    ///   [a] => (list (` a)) transformed further recursively
    ///
    /// </pre> </summary>
    private Val ConvertBackquote(Cons cons)
    {
        Symbol first = cons.First.AsSymbolOrNull;
        if (first is not { Name: "`" })
        {
            throw new ParserError($"Unexpected {first} in place of backquote");
        }

        // (` e) where e is atomic => e
        Cons body = cons.Second.AsConsOrNull;
        if (body == null)
        {
            return Cons.MakeList(_global.Intern("quote"), cons.Second);
        }

        // (` (, e)) => e
        if (IsSymbolWithName(body.First, ","))
        {
            return body.Second;
        }

        // we didn't match any special forms, just do a list match
        // (` (a ...)) => (append [a] ...)
        List<Val> forms = [];
        Cons c = body;
        while (c != null)
        {
            forms.Add(ConvertBackquoteElement(c.First));
            c = c.Rest.AsConsOrNull;
        }

        Cons result = new(_global.Intern("append"), Cons.MakeList(forms));

        // now do a quick optimization: if the result is of the form:
        // (append (list ...) (list ...) ...) where all entries are known to be lists,
        // convert this to (list ... ... ...)
        return TryOptimizeAppend(result);
    }

    /// <summary>
    /// Performs a single bracket substitution for the backquote:
    ///
    /// [(, a)] => (list a)
    /// [(,@ a)] => a
    /// [a] => (list (` a))
    /// </summary>
    private Val ConvertBackquoteElement(Val value)
    {
        Cons cons = value.AsConsOrNull;
        if (cons != null && cons.First.IsSymbol)
        {
            Symbol sym = cons.First.AsSymbol;
            switch (sym.Name)
            {
                case ",":
                    // [(, a)] => (list a)
                    return new Val(new Cons(new Val(_global.Intern("list")), cons.Rest));
                case ",@":
                    // [(,@ a)] => a
                    return cons.Second;
            }
        }

        // [a] => (list (` a)), recursively
        Cons body = Cons.MakeList(_global.Intern("`"), value);
        return Cons.MakeList(_global.Intern("list"), ConvertBackquote(body));
    }

    /// <summary>
    /// If the results form follows the pattern (append (list a b) (list c d) ...)
    /// it will be converted to a simple (list a b c d ...)
    /// </summary>
    private Val TryOptimizeAppend(Cons value)
    {
        Val original = new(value);

        if (!IsSymbolWithName(value.First, "append"))
        {
            return original;
        }

        List<Val> results = [];
        Val rest = value.Rest;
        while (rest.IsNotNil)
        {
            Cons cons = rest.AsConsOrNull;
            if (cons == null)
            {
                return original; // not a proper list
            }

            Cons maybeList = cons.First.AsConsOrNull;
            if (maybeList == null)
            {
                return original; // not all elements are lists themselves
            }

            if (!IsSymbolWithName(maybeList.First, "list"))
            {
                return original; // not all elements are of the form (list ...)
            }

            Val ops = maybeList.Rest;
            while (ops.IsCons)
            {
                results.Add(ops.AsCons.First);
                ops = ops.AsCons.Rest;
            }
            rest = cons.Rest;
        }

        // we've reassembled the bodies, return them in the form (list ...)
        return new Val(new Cons(_global.Intern("list"), Cons.MakeList(results)));
    }

    /// <summary> Convenience function: checks if the value is of type Symbol, and has the specified name </summary>
    private static bool IsSymbolWithName(Val value, string fullName) =>
        value.AsSymbolOrNull?.FullName == fullName;
}
