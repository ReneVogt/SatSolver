using System.Text;

namespace Revo.SatSolver.BooleanAlgebra;

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

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        format ??= "N";
        if (format != "N" && format != "P") throw new FormatException($"Format '{format}' is not supported.");
        
        var builder = new StringBuilder();
        ExpressionTreeWriter.Write(this, builder, format == "P");
        return builder.ToString();
    }
}
