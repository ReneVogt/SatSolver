using Revo.SatSolver.Properties;
using System.Text;

namespace Revo.SatSolver.Dimacs;

public sealed class CnfParserException : Exception
{
    public enum Reason
    {
        Unknown = 0,
        InvalidLine,
        InvalidCharacter,
        MissingProblemLine,
        InvalidProblemFormat,
        LiteralOutOfRange,
        MissingClauses,
        MissingLiteral,
        MissingTerminator
    }

    public int Line { get; }
    public int Column { get; }
    public Reason Error { get; }

    CnfParserException(string message, int line, int column, Reason error) : base(message) => (Line, Column, Error) = (line, column, error);

    static readonly CompositeFormat _invalidLine = CompositeFormat.Parse(Resources.CnfParserException_InvalidLine);
    internal static CnfParserException InvalidLine(int line) => new(string.Format(null, _invalidLine, line), line, 0, Reason.InvalidLine);

    static readonly CompositeFormat _invalidCharacter = CompositeFormat.Parse(Resources.CnfParserException_InvalidCharacter);
    internal static CnfParserException InvalidCharacter(string text, int line, int column) => new(string.Format(null, _invalidCharacter, text, line, column), line, column, Reason.InvalidCharacter);


    static readonly CompositeFormat _invalidProblemLine = CompositeFormat.Parse(Resources.CnfParserException_InvalidProblemLine);
    internal static CnfParserException InvalidProblemLine(int line, int column = 0) => new(string.Format(null, _invalidProblemLine, line), line, column, Reason.MissingProblemLine);

    static readonly CompositeFormat _invalidProblemFormat = CompositeFormat.Parse(Resources.CnfParserException_InvalidProblemFormat);
    internal static CnfParserException InvalidProblemFormat(string format, int line) => new(string.Format(null, _invalidProblemFormat, format, line), line, 1, Reason.InvalidProblemFormat);


    static readonly CompositeFormat _literalOutOfRange = CompositeFormat.Parse(Resources.CnfParserException_LiteralOutOfRange);
    internal static CnfParserException LiteralOutOfRange(int literal, int max, int line, int column) => new(string.Format(null, _literalOutOfRange, literal, max, line, column), line, column, Reason.LiteralOutOfRange);

    static readonly CompositeFormat _missingliteral = CompositeFormat.Parse(Resources.CnfParserException_MissingLiteral);
    internal static CnfParserException MissingLiteral(int line, int column) => new(string.Format(null, _missingliteral, line, column), line, column, Reason.MissingLiteral);

    static readonly CompositeFormat _missingTerminator = CompositeFormat.Parse(Resources.CnfParserException_MissingTerminator);
    internal static CnfParserException MissingTerminator(int line, int column) => new(string.Format(null, _missingTerminator, line, column), line, column, Reason.MissingTerminator);

    static readonly CompositeFormat _missingClauses = CompositeFormat.Parse(Resources.CnfParserException_MissingClauses);
    internal static CnfParserException MissingClauses(int missingClauses, int line) => new(string.Format(null, _missingClauses, missingClauses, line), line, 0, Reason.MissingClauses);
}
