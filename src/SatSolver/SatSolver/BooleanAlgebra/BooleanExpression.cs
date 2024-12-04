using System.Text;

namespace Revo.SatSolver.BooleanAlgebra;

/// <summary>
/// Abstract base class for expressions in a boolean expression tree.
/// </summary>
public abstract class BooleanExpression
{
    /// <summary>
    /// The kind of this expression.
    /// </summary>
    public abstract ExpressionKind Kind { get; }

    protected BooleanExpression() { }

    public override string ToString()
    {
        var builder = new StringBuilder();
        ExpressionTreeWriter.Write(this, builder);
        return builder.ToString();
    }
}
