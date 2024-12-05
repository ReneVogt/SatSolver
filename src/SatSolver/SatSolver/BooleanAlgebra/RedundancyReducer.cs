namespace Revo.SatSolver.BooleanAlgebra;

/// <summary>
/// Reduces redundancies in <see cref="BooleanExpression"/>s.
/// </summary>
public class RedundancyReducer : BooleanExpressionRewriter
{
    RedundancyReducer()
    {

    }

    /// <summary>
    /// Removes redundancies from a boolean <paramref name="expression"/> expression.
    /// Removes redundant literals (as in 'a | a' or 'a & a'), converts tautologies
    /// (like 'a | !a') and contradictions (like 'a & !a') to the respective constant
    /// expressions and removes supersets of literals in OR-expressions (like 
    /// '(a | b) & (a | b | c) => (a | b)').
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to reduce.</param>
    /// <returns>A <see cref="BooleanExpression"/> representing the reduced expression tree.</returns>
    /// <exception cref="ArgumentNullException">The <paramref name="expression"/> is <c>null</c>.</exception>
    public static BooleanExpression Reduce(BooleanExpression expression) =>
        new RedundancyReducer().Rewrite(expression);
}
