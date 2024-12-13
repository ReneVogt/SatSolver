namespace Revo.BooleanAlgebra.Expressions;

/// <summary>
/// An unary expression in a <see cref="BooleanExpression"/> tree.
/// Currently only NOT (!) is supported.
/// </summary>
/// <param name="op">The operator kine (current only NOT is supported).</param>
/// <param name="expression">The <see cref="BooleanExpression"/> this operator is applied to.</param>
public sealed class UnaryExpression(UnaryOperator op, BooleanExpression expression) : BooleanExpression
{
    /// <inheritdoc/>
    public override ExpressionKind Kind => ExpressionKind.Unary;

    /// <summary>
    /// The operator kind of this expression.
    /// </summary>
    public UnaryOperator Operator { get; } = op;

    /// <summary>
    /// The <see cref="BooleanExpression"/> this operator is applied to.
    /// </summary>
    public BooleanExpression Expression { get; } = expression ?? throw new ArgumentNullException(nameof(expression));
}
