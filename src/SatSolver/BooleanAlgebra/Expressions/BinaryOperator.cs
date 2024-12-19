namespace Revo.BooleanAlgebra.Expressions;

/// <summary>
/// Binary operators for <see cref="BinaryExpression"/>s.
/// </summary>
public enum BinaryOperator
{
    /// <summary>
    /// Unknown operator.
    /// </summary>
    Unknown,

    /// <summary>
    /// AND operator (&).
    /// </summary>
    And,

    /// <summary>
    /// OR operator (|).
    /// </summary>
    Or,

    /// <summary>
    /// XOR operator (%).
    /// </summary>
    Xor,

    /// <summary>
    /// Implication (>).
    /// </summary>
    Implication,

    /// <summary>
    /// Reverse implication (<)
    /// </summary>
    ReverseImplication,

    /// <summary>
    /// Equality operator (=).
    /// </summary>
    Equivalence
}
