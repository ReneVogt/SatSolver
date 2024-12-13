namespace Revo.BooleanAlgebra.Expressions;

/// <summary>
/// A constant expression in a <see cref="BooleanExpression"/> tree.
/// This can either be a '1' (or true) or a '0' (or false)..
/// </summary>
/// <param name="sense">The sense/value of this expression.</param>
public sealed class ConstantExpression(bool sense) : BooleanExpression
{
    /// <inheritdoc/>
    public override ExpressionKind Kind => ExpressionKind.Constant;

    /// <summary>
    /// The boolean value of this expression..
    /// </summary>
    public bool Sense { get; } = sense;
}
