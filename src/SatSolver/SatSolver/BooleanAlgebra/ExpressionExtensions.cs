namespace Revo.SatSolver.BooleanAlgebra;
public static class ExpressionExtensions
{
    /// <summary>
    /// Converts a <see cref="BooleanExpression"/> into a
    /// conjunctive normal form, tries to reduce redundancies
    /// and returns the expression as a <see cref="Problem"/>
    /// to be processed the <see cref="SatSolver"/>.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to transform.</param>
    /// <returns>A <see cref="Problem"/> representation of the <paramref name="expression"/>
    /// that can be processed by the <see cref="SatSolver"/>.</returns>
    /// <exception cref="ArgumentNullException">The <paramref name="expression"/> is <c>null</c>.</exception>
    public static Problem ToProblem(this BooleanExpression expression) => ExpressionToProblemConverter.ToProblem(expression);

    /// <summary>
    /// Converts a <see cref="BooleanExpression"/> into a
    /// conjunctive normal form, tries to reduce redundancies
    /// and returns the expression as a <see cref="Problem"/>
    /// to be processed the <see cref="SatSolver"/>.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to transform.</param>
    /// <param name="literalMapping">Receives the mapping from variable names to literal IDs.</param>
    /// <returns>A <see cref="Problem"/> representation of the <paramref name="expression"/>
    /// that can be processed by the <see cref="SatSolver"/>.</returns>
    /// <exception cref="ArgumentNullException">The <paramref name="expression"/> is <c>null</c>.</exception>
    public static Problem ToProblem(this BooleanExpression expression, out IReadOnlyDictionary<string, int> literalMapping) => ExpressionToProblemConverter.ToProblem(expression, out literalMapping);
}
