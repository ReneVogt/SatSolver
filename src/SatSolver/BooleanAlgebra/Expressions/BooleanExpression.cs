using Revo.BooleanAlgebra.Transformers;

namespace Revo.BooleanAlgebra.Expressions;

/// <summary>
/// Abstract base class for expressions in a boolean expression tree.
/// </summary>
public abstract class BooleanExpression : IFormattable
{
    /// <summary>
    /// The kind of this expression.
    /// </summary>
    public abstract ExpressionKind Kind { get; }

    protected BooleanExpression() { }

    public override string ToString() => ToString(null, null);

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        format ??= "N";
        if (format != "N" && format != "P") throw new FormatException($"Format '{format}' is not supported.");

        return ExpressionTreeWriter.Write(this, format == "P");
    }
}
