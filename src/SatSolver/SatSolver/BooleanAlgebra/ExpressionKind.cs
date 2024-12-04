namespace Revo.SatSolver.BooleanAlgebra;

/// <summary>
/// The kind of a <see cref="BooleanExpression"/>.
/// The main purpose is to reduce reflection when
/// handling expression trees.
/// </summary>
public enum ExpressionKind
{
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
