using Microsoft.VisualBasic;
using System.Net.Mail;
using static Revo.SatSolver.BooleanAlgebra.BooleanExpressionException;
using static Revo.SatSolver.BooleanAlgebra.ExpressionFactory;

namespace Revo.SatSolver.BooleanAlgebra;

/// <summary>
/// This transformer takes a <see cref="BooleanExpression"/> and converts
/// it into a conjunctive normal form.
/// </summary>
public class ConjunctiveNormalFormTransformer : BooleanExpressionRewriter
{

    bool _alwaysTrue;
    HashSet<string> _positives = [];
    HashSet<string> _negatives = [];

    protected ConjunctiveNormalFormTransformer()
    {
    }

    /// <summary>
    /// Rewrites this <see cref="BinaryExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="BinaryExpression"/> to transform.</param>
    /// <returns>The resulting <see cref="BooleanExpression"/> in conjunctive normal form.</returns>
    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression) => expression.Operator switch
    {
        BinaryOperator.Or => RewriteOr(expression),
        BinaryOperator.And => RewriteAnd(expression),
        _ => throw UnsupportedBinaryOperator(expression.Operator)
    };
    BooleanExpression RewriteOr(BinaryExpression expression)
    {
        var savedTrue = _alwaysTrue;

        _alwaysTrue = false;
        var left = Rewrite(expression.Left);
        var leftAlwaysTrue = _alwaysTrue;
        _alwaysTrue = false;
        var right = Rewrite(expression.Right);
        var rightAlwaysTrue = _alwaysTrue;
        _alwaysTrue = savedTrue;

        if (left.Kind == ExpressionKind.Binary)
        {
            var leftBinary = (BinaryExpression)left;
            if (leftBinary.Operator == BinaryOperator.And)
                return Rewrite(Distribute(right, leftBinary));
            if (leftBinary.Operator != BinaryOperator.Or) throw UnsupportedBinaryOperator(leftBinary.Operator);
        }

        if (right.Kind == ExpressionKind.Binary)
        {
            var rightBinary = (BinaryExpression)right;
            if (rightBinary.Operator == BinaryOperator.And)
                return Rewrite(Distribute(left, rightBinary));
            if (rightBinary.Operator != BinaryOperator.Or) throw UnsupportedBinaryOperator(rightBinary.Operator);
        }

        _alwaysTrue |= leftAlwaysTrue || rightAlwaysTrue || _negatives.Intersect(_positives).Any();
        var literals = _positives.Select(name => (BooleanExpression)Literal(name)).Concat(_negatives.Select(name => Not(Literal(name)))).ToArray();
        if (literals.Length == 1) return literals[0];
        BooleanExpression result = literals[^1];
        for (var i = literals.Length-2; i>=0; i--)
            result = literals[i].Or(result);

        return result;
    }

    BooleanExpression RewriteAnd(BinaryExpression expression)
    {
        _alwaysTrue = false;
        _positives.Clear();
        _negatives.Clear();
        var left = Rewrite(expression.Left);
        var leftAlwaysTrue = _alwaysTrue;
        var leftPositives = _positives;
        var leftNegatives = _negatives;

        _alwaysTrue = false;
        _positives = [];
        _negatives = [];
        var right = Rewrite(expression.Right);
        var rightAlwaysTrue = _alwaysTrue;

        _alwaysTrue = leftAlwaysTrue && rightAlwaysTrue;
        if (leftAlwaysTrue)
            return right;
        if (rightAlwaysTrue || leftPositives.SetEquals(_positives) && leftNegatives.SetEquals(_negatives))
            return left;

        return left == expression.Left && right == expression.Right ? expression : left.And(right);
    }

    /// <summary>
    /// Applies the distributive law to an boolean expression.
    /// </summary>
    /// <param name="factor">The part that will be distributed over the <paramref name="sum"/>.</param>
    /// <param name="sum">The part in parentheses that the <paramref name="factor"/> gets distributed over.</param>
    /// <returns>'factor & (sum1 | sum2)' -> '(factor | sum1) & (factor | sum2)' or
    /// 'factor | (sum1 & sum2)' -> '(factor & sum1) | (factor & sum2)'</returns>
    static BinaryExpression Distribute(BooleanExpression factor, BinaryExpression sum) =>
        sum.Operator switch
        {
            BinaryOperator.And => factor.Or(sum.Left).And(factor.Or(sum.Right)),
            BinaryOperator.Or => factor.And(sum.Left).Or(factor.And(sum.Right)),
            _ => throw UnsupportedBinaryOperator(sum.Operator)
        };

    /// <summary>
    /// Rewrites this <see cref="UnaryExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="UnaryExpression"/> to transform.</param>
    /// <returns>The resulting <see cref="BooleanExpression"/> in conjunctive normal form.</returns>
    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)     
    {
        if (expression.Operator != UnaryOperator.Not) throw UnsupportedUnaryOperator(expression.Operator);
        if (expression.Expression.Kind == ExpressionKind.Literal)
        {
            _negatives.Add(((LiteralExpression)expression.Expression).Name);
            return expression;
        }
        if (expression.Expression.Kind == ExpressionKind.Unary)
        {
            var unary = (UnaryExpression)expression.Expression;
            if (unary.Operator !=  UnaryOperator.Not) throw UnsupportedUnaryOperator(unary.Operator); 
            return Rewrite(unary.Expression);
        }

        if (expression.Expression.Kind != ExpressionKind.Binary) throw UnsupportedExpressionKind(expression.Expression.Kind);
        
        var binary = (BinaryExpression)expression.Expression;
        if (binary.Operator != BinaryOperator.And && binary.Operator != BinaryOperator.Or) throw UnsupportedBinaryOperator(binary.Operator);

        return Rewrite(binary.Operator switch
        {
            BinaryOperator.And => Not(binary.Left).Or(Not(binary.Right)),
            BinaryOperator.Or => Not(binary.Left).And(Not(binary.Right)),
            _ => throw UnsupportedBinaryOperator(binary.Operator)
        });
    }

    public override BooleanExpression RewriteLiteralExpression(LiteralExpression expression)
    {
        _positives.Add(expression.Name);
        return expression;
    }

    /// <summary>
    /// Transforms the given <see cref="BooleanExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to transform.</param>
    /// <returns>A <see cref="BooleanExpression"/> representing the conjunctive normal form of the original <paramref name="expression"/> or <c>null</c> if the expression always evaluates to <c>true</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <c>null</c>.</exception>
    public static BooleanExpression? Transform(BooleanExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        var transformer = new ConjunctiveNormalFormTransformer();
        var result = transformer.Rewrite(expression);
        return transformer._alwaysTrue ? null : result;
    }
}
