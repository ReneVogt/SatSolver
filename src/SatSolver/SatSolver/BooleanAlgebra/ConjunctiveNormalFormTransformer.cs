namespace Revo.SatSolver.BooleanAlgebra;

/// <summary>
/// This transformer takes a <see cref="BooleanExpression"/> and converts
/// it into a conjunctive normal form.
/// </summary>
public sealed class ConjunctiveNormalFormTransformer : BooleanExpressionRewriter
{
    ConjunctiveNormalFormTransformer()
    {
    }

    /// <summary>
    /// Rewrites this <see cref="BinaryExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="BinaryExpression"/> to transform.</param>
    /// <returns>The resulting <see cref="BooleanExpression"/> in conjunctive normal form.</returns>
    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression) => base.RewriteBinaryExpression(expression);

    /// <summary>
    /// Rewrites this <see cref="UnaryExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="UnaryExpression"/> to transform.</param>
    /// <returns>The resulting <see cref="BooleanExpression"/> in conjunctive normal form.</returns>
    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression) => base.RewriteUnaryExpression(expression);

    /// <summary>
    /// Memorizes the given literal to later reduce redundancies and tautologies
    /// in the conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="LiteralExpression"/> to visit.</param>
    /// <returns>The original <see cref="LiteralExpression"/> without changes.</returns>
    public override BooleanExpression RewriteLiteralExpression(LiteralExpression expression) => base.RewriteLiteralExpression(expression);

    /// <summary>
    /// Transforms the given <see cref="BooleanExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to transform.</param>
    /// <returns>A <see cref="BooleanExpression"/> representing the conjunctive normal form of the original <paramref name="expression"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <c>null</c>.</exception>
    public static BooleanExpression Transform(BooleanExpression expression) => new ConjunctiveNormalFormTransformer().Rewrite(expression ?? throw new ArgumentNullException(nameof(expression)));
}
