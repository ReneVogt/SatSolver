using Revo.SatSolver.Properties;
using System.Text;

namespace Revo.SatSolver;

/// <summary>
/// Exception thrown by the <see cref="DimacsCnfParser"/> when encountering
/// mistakes in the DIMACS input.
/// </summary>
public sealed class CnfParserException : Exception
{
    public enum Reason
    {
        Unknown = 0,

        /// <summary>
        /// An expected problem definition line is missing or invalid.
        /// </summary>
        InvalidProblemLine,

        /// <summary>
        /// The format specified by a problem definition is not supported.
        /// Currently only 'cnf' is supported.
        /// </summary>
        InvalidProblemFormat,

        /// <summary>
        /// A literal id was larger then the specified number of literals.
        /// </summary>
        LiteralOutOfRange,

        /// <summary>
        /// A literal or clause termination ('0') was expected but not found.
        /// </summary>
        MissingLiteral
    }

    public int Line { get; }
    public int Position { get; }
    public Reason Error { get; }

    CnfParserException(string message, int line, int position, Reason error) : base(message) => (Line, Position, Error) = (line, position, error);

    static readonly CompositeFormat _invalidProblemLine = CompositeFormat.Parse(Resources.CnfParserException_InvalidProblemLine);
    internal static CnfParserException InvalidProblemLine(int line, int position = 0) => new(string.Format(null, _invalidProblemLine, line), line, position, Reason.InvalidProblemLine);

    static readonly CompositeFormat _invalidProblemFormat = CompositeFormat.Parse(Resources.CnfParserException_InvalidProblemFormat);
    internal static CnfParserException InvalidProblemFormat(string format, int line, int position) => new(string.Format(null, _invalidProblemFormat, format, line, position), line, position, Reason.InvalidProblemFormat);


    static readonly CompositeFormat _literalOutOfRange = CompositeFormat.Parse(Resources.CnfParserException_LiteralOutOfRange);
    internal static CnfParserException LiteralOutOfRange(int literal, int max, int line, int position) => new(string.Format(null, _literalOutOfRange, literal, max, line, position), line, position, Reason.LiteralOutOfRange);

    static readonly CompositeFormat _missingliteral = CompositeFormat.Parse(Resources.CnfParserException_MissingLiteral);
    internal static CnfParserException MissingLiteral(int clause, int expectedClauses, int line, int position) => new(string.Format(null, _missingliteral, clause, expectedClauses, line, position), line, position, Reason.MissingLiteral);
}
