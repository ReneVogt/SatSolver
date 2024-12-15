using Revo.BooleanAlgebra.Expressions;
using Revo.BooleanAlgebra.Transformers;
using static Revo.BooleanAlgebra.Expressions.ExpressionFactory;

namespace Revo.SatSolver.Parsing;

/// <summary>
/// This transformer takes a <see cref="BooleanExpression"/> and converts
/// it into a conjunctive normal form using Tseitin transformation.
/// </summary>
public class TseitinTransformer : BooleanExpressionRewriter
{
    protected TseitinTransformer()
    {
    }

    /// <summary>
    /// Rewrites this <see cref="BinaryExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="BinaryExpression"/> to transform.</param>
    /// <returns>The resulting <see cref="BooleanExpression"/> in conjunctive normal form.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <c>null</c>.</exception>
    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));

        //
        // AND expressions don't need extra treatment, only their children
        // need rewriting.
        //
        if (expression.Operator == BinaryOperator.And) return base.RewriteBinaryExpression(expression);

        throw new NotImplementedException();
    }

    /// <summary>
    /// Rewrites this <see cref="UnaryExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="UnaryExpression"/> to transform.</param>
    /// <returns>The resulting <see cref="BooleanExpression"/> in conjunctive normal form.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <c>null</c>.</exception>
    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));

        //
        // If this NOT simply negates a constant, we can
        // return the negated constant.
        //
        if (expression.Expression.Kind == ExpressionKind.Constant && expression.Expression is ConstantExpression { Sense: var sense })
            return sense ? Zero : One;
        //
        // If this NOT simply negates a literal, we can
        // return the expression unchanged.
        //
        if (expression.Expression.Kind == ExpressionKind.Literal)
            return expression;

        //
        // If this is a double negation it can be skipped
        // and the inner epression should be rewritten and
        // returned.
        //
        if (expression.Expression.Kind == ExpressionKind.Unary && expression.Expression is UnaryExpression { Expression: var doubleNegatedExpression })
            return Rewrite(doubleNegatedExpression);

        //
        // So we have a binary expression and rewrite it according to 
        // De Morgan's laws:
        // !(a | b) = !a & !b
        // !(a & b) = !a | !b
        // and the resulting expression is rewritten again.
        //
        var binary = (BinaryExpression)expression.Expression;
        return Rewrite(binary.Operator == BinaryOperator.And ? Not(binary.Left).Or(Not(binary.Right)) : Not(binary.Left).And(Not(binary.Right)));
    }

    /// <summary>
    /// Transforms the given <see cref="BooleanExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to transform.</param>
    /// <returns>A <see cref="BooleanExpression"/> representing the conjunctive normal form of the original <paramref name="expression"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <c>null</c>.</exception>
    public static BooleanExpression Transform(BooleanExpression expression) => new TseitinTransformer().Rewrite(Lowerer.Lower(expression));
}
