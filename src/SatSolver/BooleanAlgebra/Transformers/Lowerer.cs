using Revo.BooleanAlgebra.Expressions;
using static Revo.BooleanAlgebra.Expressions.BooleanExpressionException;
using static Revo.BooleanAlgebra.Expressions.ExpressionFactory;

namespace Revo.BooleanAlgebra.Transformers;

/// <summary>
/// This transformer takes a <see cref="BooleanExpression"/> and removes
/// all operations "higher" than AND, OR and NOT.
/// So for the moment, a % b will be lowererd to (a | b) & (!a | !b).
/// a = b will be lowered to (!a | b) & (a | !b).
/// Double negations will be removed.
/// Constant expressions will be propagated.
/// </summary>
public class Lowerer : BooleanExpressionRewriter
{
    protected Lowerer()
    {
    }

    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        if (expression.Operator != UnaryOperator.Not) throw UnsupportedUnaryOperator(expression.Operator);

        //
        // If the inner expression is a constant, we
        // return the negated constant.
        //
        if (expression.Expression.Kind == ExpressionKind.Constant && expression.Expression is ConstantExpression { Sense: var sense })
            return sense ? Zero : One;

        //
        // If this is a double negation it can be skipped
        // and the inner epression should be rewritten and
        // returned.
        //
        if (expression.Expression.Kind == ExpressionKind.Unary && expression.Expression is UnaryExpression { Operator: var op, Expression: var doubleNegatedExpression })
        {
            if (op !=  UnaryOperator.Not) throw UnsupportedUnaryOperator(op);
            return Rewrite(doubleNegatedExpression);
        }

        return base.RewriteUnaryExpression(expression);
    }

    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));

        //
        // XOR expressions like a % b are rewritten as (a | b) & (!a | !b) and 
        // this converted expression is rewritten again to transform children.
        //
        if (expression.Operator == BinaryOperator.Xor)
            return Rewrite(expression.Left.Or(expression.Right).And(Not(expression.Left).Or(Not(expression.Right))));

        //
        // Equivalence expressions like a = b are rewritten as (!a | b) & (a | !b).
        //
        if (expression.Operator == BinaryOperator.Equivalence)
            return Rewrite(Not(expression.Left).Or(expression.Right).And(expression.Left.Or(Not(expression.Right))));

        //
        // Implication expressions like a > b are rewritten as !a | b.
        //
        if (expression.Operator == BinaryOperator.Implication)
            return Rewrite(Not(expression.Left)).Or(expression.Right);

        //
        // Reverse implication expressions like a < b are rewritten as a | !b.
        //
        if (expression.Operator == BinaryOperator.ReverseImplication)
            return Rewrite(expression.Left.Or(Not(expression.Right)));


        if (expression.Operator != BinaryOperator.Or && expression.Operator != BinaryOperator.And) throw UnsupportedBinaryOperator(expression.Operator);

        var left = Rewrite(expression.Left);
        if (left.Kind == ExpressionKind.Constant && left is ConstantExpression { Sense: var leftSense })
        {
            if (expression.Operator == BinaryOperator.And)
            {
                if (!leftSense) return Zero;
                return Rewrite(expression.Right);
            }
            if (expression.Operator == BinaryOperator.Or)
            {
                if (leftSense) return One;
                return Rewrite(expression.Right);
            }
        }

        var right = Rewrite(expression.Right);
        if (right.Kind == ExpressionKind.Constant && right is ConstantExpression { Sense: var rightSense })
        {
            if (expression.Operator == BinaryOperator.And)
            {
                if (!rightSense) return Zero;
                return left;
            }
            if (expression.Operator == BinaryOperator.Or)
            {
                if (rightSense) return One;
                return Rewrite(left);
            }
        }

        return left == expression.Left && right == expression.Right 
            ? expression
            : expression.Operator == BinaryOperator.And ? left.And(right) : left.Or(right);
    }

    /// <summary>
    /// Transforms the given <see cref="BooleanExpression"/> to use only OR, AND and NOT operations.
    /// It also propagates constants and removes double negations.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to transform.</param>
    /// <returns>The lowered <see cref="BooleanExpression"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <c>null</c>.</exception>
    public static BooleanExpression Lower(BooleanExpression expression) => new Lowerer().Rewrite(expression);
}
