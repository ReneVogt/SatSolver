using static Revo.SatSolver.BooleanAlgebra.BooleanExpressionException;
using static Revo.SatSolver.BooleanAlgebra.ExpressionFactory;

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
        _ = expression ?? throw new ArgumentNullException(nameof(expression));

        //
        // AND expressions don't need extra treatment, only their children
        // need rewriting.
        //
        if (expression.Operator == BinaryOperator.And) return base.RewriteBinaryExpression(expression);
        if (expression.Operator != BinaryOperator.Or) throw UnsupportedBinaryOperator(expression.Operator);

        // rewrite children
        var left = Rewrite(expression.Left);
        var right = Rewrite(expression.Right);

        //
        // If one or both of the children are AND expressions
        // they need to be "distributed" so that the current
        // expression becomes an AND expression of OR expressions.
        // This result of course needs to be rewritten again.
        //
        return (left, right) switch
        {
            (BinaryExpression { Operator: BinaryOperator.And } leftBinary, BinaryExpression { Operator: BinaryOperator.And } rightBinary) =>
                //
                // (a & b) | (c & d) = (a | c) & (a | d) & (b | c) & (b | d)
                //
                Rewrite(
                    leftBinary.Left.Or(rightBinary.Left)
                    .And(leftBinary.Left.Or(rightBinary.Right))
                    .And(leftBinary.Right.Or(rightBinary.Left))
                    .And(leftBinary.Right.Or(rightBinary.Right))),
            (BinaryExpression { Operator: BinaryOperator.And } leftBinary, _) =>
                //
                // (a & b) | c = (a | c) & (b | c)
                // 
                Rewrite(
                    leftBinary.Left.Or(right)
                    .And(leftBinary.Right.Or(right))),
            (_, BinaryExpression { Operator: BinaryOperator.And } rightBinary) =>
                //
                // a | (b & c) = (a | b) & (a | c)
                // 
                Rewrite(
                    left.Or(rightBinary.Left)
                    .And(left.Or(rightBinary.Right))),
            _ =>  left == expression.Left && right == expression.Right ? expression : left.Or(right)
        };
    }

    /// <summary>
    /// Rewrites this <see cref="UnaryExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="UnaryExpression"/> to transform.</param>
    /// <returns>The resulting <see cref="BooleanExpression"/> in conjunctive normal form.</returns>
    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)     
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        if (expression.Operator != UnaryOperator.Not) throw UnsupportedUnaryOperator(expression.Operator);

        //
        // If this NOT simply negates a literal or constant the expression 
        // can be simply returned without changes.
        //
        if (expression.Expression.Kind == ExpressionKind.Literal || expression.Expression.Kind == ExpressionKind.Constant)
            return expression;

        //
        // If this is a double negation it can be skipped
        // and the inner epression should be rewritten and
        // returned.
        //
        if (expression.Expression.Kind == ExpressionKind.Unary)
        {
            var unary = (UnaryExpression)expression.Expression;
            if (unary.Operator !=  UnaryOperator.Not) throw UnsupportedUnaryOperator(unary.Operator); 
            return Rewrite(unary.Expression);
        }

        // So it must be a binary expression.
        if (expression.Expression.Kind != ExpressionKind.Binary) throw UnsupportedExpressionKind(expression.Expression.Kind);
        
        var binary = (BinaryExpression)expression.Expression;
        if (binary.Operator != BinaryOperator.And && binary.Operator != BinaryOperator.Or) throw UnsupportedBinaryOperator(binary.Operator);

        //
        // The expression is rewritten according to boolean algebra rules:
        // !(a | b) = !a & !b
        // !(a & b) = !a | !b
        // and the resulting expression is rewritten again.
        return Rewrite(binary.Operator switch
        {
            BinaryOperator.And => Not(binary.Left).Or(Not(binary.Right)),
            BinaryOperator.Or => Not(binary.Left).And(Not(binary.Right)),
            _ => throw UnsupportedBinaryOperator(binary.Operator)
        });
    }

    /// <summary>
    /// Transforms the given <see cref="BooleanExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to transform.</param>
    /// <returns>A <see cref="BooleanExpression"/> representing the conjunctive normal form of the original <paramref name="expression"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <c>null</c>.</exception>
    public static BooleanExpression Transform(BooleanExpression expression) => new ConjunctiveNormalFormTransformer().Rewrite(expression);
}
