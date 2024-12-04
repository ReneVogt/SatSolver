using System.Diagnostics;
using static Revo.SatSolver.BooleanAlgebra.BooleanExpressionException;

namespace Revo.SatSolver.BooleanAlgebra;

/// <summary>
/// This transformer takes a <see cref="BooleanExpression"/> and converts
/// it into a conjunctive normal form.
/// </summary>
public class ConjunctiveNormalFormTransformer : BooleanExpressionRewriter
{
    protected ConjunctiveNormalFormTransformer()
    {
    }

    /// <summary>
    /// Rewrites this <see cref="BinaryExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="BinaryExpression"/> to transform.</param>
    /// <returns>The resulting <see cref="BooleanExpression"/> in conjunctive normal form.</returns>
    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression)
    {
        if (expression.Operator == BinaryOperator.And) return base.RewriteBinaryExpression(expression);
        if (expression.Operator != BinaryOperator.Or) throw UnsupportedBinaryOperator(expression.Operator);

        var left = Rewrite(expression.Left);
        var right = Rewrite(expression.Right);

        if ((left.Kind == ExpressionKind.Literal || left.Kind == ExpressionKind.Unary) &&
            (right.Kind == ExpressionKind.Literal || right.Kind == ExpressionKind.Unary))
            return left == expression.Left && right == expression.Right
                ? expression
                : new BinaryExpression(left, BinaryOperator.Or, right);

        if (left.Kind == ExpressionKind.Binary)
        {
            var leftBinary = (BinaryExpression)left;
            if (leftBinary.Operator == BinaryOperator.And)
                return Distribute(right, leftBinary);
            if (leftBinary.Operator != BinaryOperator.Or) throw UnsupportedBinaryOperator(leftBinary.Operator);
        }
        if (right.Kind != ExpressionKind.Binary) throw UnsupportedExpressionKind(right.Kind);

        var rightBinary = (BinaryExpression)right;
        if (rightBinary.Operator == BinaryOperator.Or)
            return left == expression.Left && right == expression.Right
                ? expression
                : new BinaryExpression (left, BinaryOperator.Or, rightBinary);

        if (rightBinary.Operator != BinaryOperator.And) throw UnsupportedBinaryOperator(rightBinary.Operator);
        return Distribute(left, rightBinary);
    }

    /// <summary>
    /// Applies the distributive law to an boolean expression.
    /// </summary>
    /// <param name="factor">The part that will be distributed over the <paramref name="sum"/>.</param>
    /// <param name="sum">The part in parentheses that the <paramref name="factor"/> gets distributed over.</param>
    /// <returns>'factor & (sum1 | sum2)' -> '(factor | sum1) & (factor | sum2)' or
    /// 'factor | (sum1 & sum2)' -> '(factor & sum1) | (factor & sum2)'</returns>
    static BinaryExpression Distribute(BooleanExpression factor, BinaryExpression sum) =>
        new BinaryExpression(
            left: new BinaryExpression(
                left: factor,
                op: sum.Operator == BinaryOperator.And ? BinaryOperator.Or : BinaryOperator.And,
                right: sum.Left),
            op: sum.Operator,
            right: new BinaryExpression(
                left: factor,
                op: sum.Operator == BinaryOperator.And ? BinaryOperator.Or : BinaryOperator.And,
                right: sum.Right));

    /// <summary>
    /// Rewrites this <see cref="UnaryExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="UnaryExpression"/> to transform.</param>
    /// <returns>The resulting <see cref="BooleanExpression"/> in conjunctive normal form.</returns>
    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)     
    {
        if (expression.Operator != UnaryOperator.Not) throw UnsupportedUnaryOperator(expression.Operator);
        if (expression.Expression.Kind == ExpressionKind.Literal) return base.RewriteUnaryExpression(expression);
        if (expression.Expression.Kind == ExpressionKind.Unary)
        {
            var unary = (UnaryExpression)expression.Expression;
            if (unary.Operator !=  UnaryOperator.Not) throw UnsupportedUnaryOperator(unary.Operator); 
            return Rewrite(unary.Expression);
        }

        if (expression.Expression.Kind != ExpressionKind.Binary) throw UnsupportedExpressionKind(expression.Expression.Kind);
        
        var binary = (BinaryExpression)expression.Expression;
        if (binary.Operator != BinaryOperator.And && binary.Operator != BinaryOperator.Or) throw UnsupportedBinaryOperator(binary.Operator);

        return Rewrite(new BinaryExpression(
            left: new UnaryExpression(UnaryOperator.Not, binary.Left),
            op: binary.Operator == BinaryOperator.Or ? BinaryOperator.And : BinaryOperator.Or,
            right: new UnaryExpression(UnaryOperator.Not, binary.Right)));
    }

    /// <summary>
    /// Transforms the given <see cref="BooleanExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to transform.</param>
    /// <returns>A <see cref="BooleanExpression"/> representing the conjunctive normal form of the original <paramref name="expression"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <c>null</c>.</exception>
    public static BooleanExpression Transform(BooleanExpression expression) => new ConjunctiveNormalFormTransformer().Rewrite(expression ?? throw new ArgumentNullException(nameof(expression)));
}
