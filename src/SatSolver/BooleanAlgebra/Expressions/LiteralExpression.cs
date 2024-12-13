namespace Revo.BooleanAlgebra.Expressions;

/// <summary>
/// A literal expression in a <see cref="BooleanExpression"/> tree.
/// Represents a literal or variable in an expression.
/// </summary>
/// <param name="name">The variable/literal name.</param>
public sealed class LiteralExpression(string name) : BooleanExpression
{
    /// <inheritdoc/>
    public override ExpressionKind Kind => ExpressionKind.Literal;

    /// <summary>
    /// The name of the literal/variable.
    /// </summary>
    public string Name { get; } = name;
}
