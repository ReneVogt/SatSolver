namespace Revo.BooleanAlgebra.Expressions;

/// <summary>
/// The kind of a <see cref="BooleanExpression"/>.
/// The main purpose is to reduce reflection when
/// handling expression trees.
/// </summary>
public enum ExpressionKind
{
    /// <summary>
    /// A <see cref="ConstantExpression"/> that 
    /// is either <c>true</c> (or '1') or
    /// <c>false</c> (or '0').
    /// </summary>
    Constant,

    /// <summary>
    /// A <see cref="LiteralExpression"/>.
    /// </summary>
    Literal,

    /// <summary>
    /// An <see cref="UnaryExpression"/>.
    /// </summary>
    Unary,

    /// <summary>
    /// A <see cref="BinaryExpression"/>.
    /// </summary>
    Binary

}
