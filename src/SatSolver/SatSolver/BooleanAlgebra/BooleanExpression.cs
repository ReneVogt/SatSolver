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
}
