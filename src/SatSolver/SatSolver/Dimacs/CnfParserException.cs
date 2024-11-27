using Revo.SatSolver.Properties;
using System.Text;

namespace Revo.SatSolver.Dimacs;

public sealed class CnfParserException : Exception
{
    public enum Reason
    {
        Unknown = 0,
        MissingProblemLine,
        InvalidProblemFormat,
        LiteralOutOfRange,
        MissingClauses,
        MissingLiteral,
        MissingTerminator,
        InvalidCharacter
    }

    public int Line { get; }
    public int Part { get; }
    public Reason Error { get; }

    CnfParserException(string message, int line, int part, Reason error) : base(message) => (Line, Part, Error) = (line, part, error);

    static readonly CompositeFormat _missingProblemLine = CompositeFormat.Parse(Resources.CnfParserException_MissingProblemLine);
    internal static CnfParserException MissingProblemLine(int line) => new(string.Format(null, _missingProblemLine, line), line, 0, Reason.MissingProblemLine);

    static readonly CompositeFormat _invalidProblemFormat = CompositeFormat.Parse(Resources.CnfParserException_InvalidProblemFormat);
    internal static CnfParserException InvalidProblemFormat(string format, int line) => new(string.Format(null, _invalidProblemFormat, format, line), line, 1, Reason.InvalidProblemFormat);


    static readonly CompositeFormat _literalOutOfRange = CompositeFormat.Parse(Resources.CnfParserException_LiteralOutOfRange);
    internal static CnfParserException LiteralOutOfRange(int literal, int max, int line, int part) => new(string.Format(null, _literalOutOfRange, literal, max, line, part), line, part, Reason.LiteralOutOfRange);

    static readonly CompositeFormat _missingliteral = CompositeFormat.Parse(Resources.CnfParserException_MissingLiteral);
    internal static CnfParserException MissingLiteral(int line, int part) => new(string.Format(null, _missingliteral, line, part), line, part, Reason.MissingLiteral);

    static readonly CompositeFormat _missingTerminator = CompositeFormat.Parse(Resources.CnfParserException_MissingTerminator);
    internal static CnfParserException MissingTerminator(int line, int part) => new(string.Format(null, _missingTerminator, line, part), line, part, Reason.MissingTerminator);

    static readonly CompositeFormat _missingClauses = CompositeFormat.Parse(Resources.CnfParserException_MissingClauses);
    internal static CnfParserException MissingClauses(int clauses, int line) => new(string.Format(null, _missingClauses, clauses), line, 0, Reason.MissingClauses);



    static readonly CompositeFormat _invalidCharacter = CompositeFormat.Parse(Resources.CnfParserException_InvalidCharacter);
    internal static CnfParserException InvalidCharacter(string text, int line, int part) => new(string.Format(null, _invalidCharacter, text, line, part), line, part, Reason.InvalidCharacter);
}
