using Revo.SatSolver.Properties;
using System.Text;

namespace Revo.SatSolver.Parsing;

/// <summary>
/// Exception thrown by the <see cref="BooleanAlgebraParser"/> when encountering
/// invalid or unexpected characters.
/// </summary>
public sealed class InvalidBooleanAlgebraException : Exception
{
    static readonly CompositeFormat _invalidSyntax = CompositeFormat.Parse(Resources.InvalidBooleanAlgebraException_InvalidCharacter);
    static readonly CompositeFormat _unexpectedEnd = CompositeFormat.Parse(Resources.InvalidBooleanAlgebraException_UnexpectedEnd);

    public enum Reason
    {
        InvalidOrUnexpectedCharacter,
        UnexpectedEnd
    }

    /// <summary>
    /// Indicates the type of error.
    /// </summary>
    public Reason Error { get; }

    /// <summary>
    /// The position of the first error in the input string.
    /// </summary>
    public int Position { get; }

    InvalidBooleanAlgebraException(string message, int position, Reason error) : base(message) => (Position, Error) = (position, error);

    internal static InvalidBooleanAlgebraException InvalidCharacter(int position) => new(string.Format(null, _invalidSyntax, position), position, Reason.InvalidOrUnexpectedCharacter);
    internal static InvalidBooleanAlgebraException UnexpectedEnd(int position) => new(string.Format(null, _unexpectedEnd, position), position, Reason.UnexpectedEnd);
}
