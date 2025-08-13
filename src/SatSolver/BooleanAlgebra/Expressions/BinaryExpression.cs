namespace Revo.BooleanAlgebra.Expressions;

/// <summary>
/// Represents a binary expression in a <see cref="BooleanExpression"/> tree.
/// </summary>
/// <param name="left">The left operand of this expression.</param>
/// <param name="op">The <see cref="BinaryOperator"/> of this expression.
/// <param name="right">The right operand of this expression.</param>
public sealed class BinaryExpression(BooleanExpression left, BinaryOperator op, BooleanExpression right) : BooleanExpression
{
    /// <inheritdoc/>
    public override ExpressionKind Kind => ExpressionKind.Binary;

    /// <summary>
    /// The left operand of this expression.
    /// </summary>
    public BooleanExpression Left { get; } = left ?? throw new ArgumentNullException(nameof(left));

    /// <summary>
    /// The operator of this expression.
    /// </summary>
    public BinaryOperator Operator { get; } = op;

    /// <summary>
    /// The right operand of this expression.
    /// </summary>
    public BooleanExpression Right { get; } = right ?? throw new ArgumentNullException(nameof(right));
}
