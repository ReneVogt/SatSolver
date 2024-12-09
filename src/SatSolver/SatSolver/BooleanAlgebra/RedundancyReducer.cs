using System.Security.AccessControl;
using static Revo.SatSolver.BooleanAlgebra.ExpressionFactory;

namespace Revo.SatSolver.BooleanAlgebra;

/// <summary>
/// Reduces redundancies in <see cref="BooleanExpression"/>s.
/// </summary>
public class RedundancyReducer : BooleanExpressionRewriter
{
    HashSet<string> _positives = [];
    HashSet<string> _negatives = [];
    HashSet<BooleanExpression> _nestedExpressions = [];

    RedundancyReducer() { }

    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)
    {
        if (expression.Operator != UnaryOperator.Not) return base.RewriteUnaryExpression(expression);

        //
        // Early out for double negations.
        //
        if (expression.Expression.Kind == ExpressionKind.Unary && expression.Expression is UnaryExpression { Operator: UnaryOperator.Not, Expression: var doubleNegatedExpression })
            return Rewrite(doubleNegatedExpression);

        var savedPositives = _positives;
        var savedNegatives = _negatives;
        var savedNestedExpressions = _nestedExpressions;
        var currentPositives = _positives = [];
        var currentNegatives = _negatives = [];
        var currentNestedExpression = _nestedExpressions = [];
        var innerExpression = Rewrite(expression.Expression);
        _positives = savedPositives;
        _negatives = savedNegatives;
        _nestedExpressions = savedNestedExpressions;

        //
        // Early out for constants.
        //
        if (innerExpression.Kind == ExpressionKind.Constant)
            return ((ConstantExpression)innerExpression).Sense ? Zero : One;

        var resultingExpression = innerExpression == expression.Expression ? expression : Not(innerExpression);

        //
        // Handle the inner expression.
        // 
        switch (innerExpression.Kind)
        {
            case ExpressionKind.Literal:
                //
                // The literal is negated, add it to the _negatives.
                // 
                _negatives.UnionWith(currentPositives);
                break;

            case ExpressionKind.Unary:
                //
                // Double negations were already filtered out, so
                // this can only happen for a negated literal.
                // Add it to _positives, because this is a NOT, too.
                _positives.UnionWith(currentNegatives);
                break;

            default:
                //
                // This must be a binary that is negated
                // by the current expression, so it will
                // stay as a nested expression and not
                // add to the literal sets.
                //
                _nestedExpressions.Add(resultingExpression);
                break;
        }

        return resultingExpression;
    }
    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression) => expression.Operator switch
    {
        BinaryOperator.And => RewriteAndExpression(expression),
        BinaryOperator.Xor => RewriteXorExpression(expression),
        BinaryOperator.Or => RewriteOrExpression(expression),
        _ => base.RewriteBinaryExpression(expression)
    };

    BooleanExpression RewriteAndExpression(BinaryExpression expression)
    {
        //
        // Rewrite children.
        //
        var savedPositives = _positives;
        var savedNegatives = _negatives;
        var savedNestedExpressions = _nestedExpressions;
        var leftPositives = _positives = [];
        var leftNegatives = _negatives = [];
        var leftNestedExpressions = _nestedExpressions = [];
        var left = Rewrite(expression.Left);
        var rightPositives = _positives = [];
        var rightNegatives = _negatives = [];
        var rightNestedExpressions = _nestedExpressions = [];
        var right = Rewrite(expression.Right);
        _positives = savedPositives;
        _negatives = savedNegatives;
        _nestedExpressions = savedNestedExpressions;

        //
        // If one of the children is a different binary expression, add it to _nestedExpressions.
        //
        var leftIsDifferentBinary = left.Kind == ExpressionKind.Binary && left is BinaryExpression { Operator: not BinaryOperator.And };
        if (leftIsDifferentBinary)
            _nestedExpressions.Add(left);
        var rightIsDifferentBinary = right.Kind == ExpressionKind.Binary && right is BinaryExpression { Operator: not BinaryOperator.And };
        if (rightIsDifferentBinary)
            _nestedExpressions.Add(right);

        //
        // Check for constants.
        //
        if (left.Kind == ExpressionKind.Constant)
        {
            if (!((ConstantExpression)left).Sense)
                return Zero;
            if (!rightIsDifferentBinary)
            {
                _positives.UnionWith(rightPositives);
                _negatives.UnionWith(rightNegatives);
                _nestedExpressions.UnionWith(rightNestedExpressions);
            }
            return right;
        }
        if (right.Kind == ExpressionKind.Constant)
        {
            if (!((ConstantExpression)right).Sense)
                return Zero;
            if (!leftIsDifferentBinary)
            {
                _positives.UnionWith(leftPositives);
                _negatives.UnionWith(leftNegatives);
                _nestedExpressions.UnionWith(leftNestedExpressions);
            }
            return left;
        }

        if (!leftIsDifferentBinary)
        {
            _positives.UnionWith(leftPositives);
            _negatives.UnionWith(leftNegatives);
            _nestedExpressions.UnionWith(leftNestedExpressions);
        }
        if (!rightIsDifferentBinary)
        {
            _positives.UnionWith(rightPositives);
            _negatives.UnionWith(rightNegatives);
            _nestedExpressions.UnionWith(rightNestedExpressions);
        }

        if (_positives.Intersect(_negatives).Any())
        {
            _positives.Clear();
            _negatives.Clear();
            _nestedExpressions.Clear();
            return Zero;
        }

        return _positives.Select(name => (BooleanExpression)Literal(name))
            .Concat(_negatives.Select(name => Not(Literal(name))))
            .Concat(_nestedExpressions).Aggregate((e1, e2) => e1.And(e2));
    }
    BooleanExpression RewriteXorExpression(BinaryExpression expression)
    {
        var savedPositives = _positives;
        var savedNegatives = _negatives;
        var savedNestedExpressions = _nestedExpressions;
        var leftPositives = _positives = [];
        var leftNegatives = _negatives = [];
        _nestedExpressions = [];
        var left = Rewrite(expression.Left);
        var rightPositives = _positives = [];
        var rightNegatives = _negatives = [];
        _nestedExpressions = [];
        var right = Rewrite(expression.Right);
        _positives = savedPositives;
        _negatives = savedNegatives;
        _nestedExpressions = savedNestedExpressions;

        //
        // Check for constants.
        //
        if (left.Kind == ExpressionKind.Constant)
            return ((ConstantExpression)left).Sense ? Rewrite(Not(right)) : Rewrite(right);
        if (right.Kind == ExpressionKind.Constant)
            return ((ConstantExpression)right).Sense ? Rewrite(Not(left)) : Rewrite(left);

        //
        // Check for XOR between simple or negated literals
        //
        switch ((leftPositives.Count, leftNegatives.Count, rightPositives.Count, rightNegatives.Count))
        {
            case (1, 0, 1, 0):
                if (leftPositives.SetEquals(rightPositives)) return Zero;
                break;
            case (1, 0, 0, 1):
                if (leftPositives.SetEquals(rightNegatives)) return One;
                break;
            case (0, 1, 1, 0):
                if (leftNegatives.SetEquals(rightPositives)) return One;
                break;
            case (0, 1, 0, 1):
                if (leftNegatives.SetEquals(rightNegatives)) return Zero;
                break;
        }

        //
        // Nothing to do.
        //
        return left == expression.Left && right == expression.Right
            ? expression
            : left.Xor(right);
    }

    BooleanExpression RewriteOrExpression(BinaryExpression expression)
    {
        //
        // Rewrite children.
        //
        var savedPositives = _positives;
        var savedNegatives = _negatives;
        var savedNestedExpressions = _nestedExpressions;
        var leftPositives = _positives = [];
        var leftNegatives = _negatives = [];
        var leftNestedExpressions = _nestedExpressions = [];
        var left = Rewrite(expression.Left);
        var rightPositives = _positives = [];
        var rightNegatives = _negatives = [];
        var rightNestedExpressions = _nestedExpressions = [];
        var right = Rewrite(expression.Right);
        _positives = savedPositives;
        _negatives = savedNegatives;
        _nestedExpressions = savedNestedExpressions;

        //
        // If one of the children is a different binary expression, add it to _nestedExpressions.
        //
        var leftIsDifferentBinary = left.Kind == ExpressionKind.Binary && left is BinaryExpression { Operator: not BinaryOperator.Or };
        if (leftIsDifferentBinary)
            _nestedExpressions.Add(left);
        var rightIsDifferentBinary = right.Kind == ExpressionKind.Binary && right is BinaryExpression { Operator: not BinaryOperator.Or };
        if (rightIsDifferentBinary)
            _nestedExpressions.Add(right);

        //
        // Check for constants.
        //
        if (left.Kind == ExpressionKind.Constant)
        {
            if (((ConstantExpression)left).Sense)
                return One;
            if (!rightIsDifferentBinary)
            {
                _positives.UnionWith(rightPositives);
                _negatives.UnionWith(rightNegatives);
                _nestedExpressions.UnionWith(rightNestedExpressions);
            }
            return right;
        }
        if (right.Kind == ExpressionKind.Constant)
        {
            if (((ConstantExpression)right).Sense)
                return One;
            if (!leftIsDifferentBinary)
            {
                _positives.UnionWith(leftPositives);
                _negatives.UnionWith(leftNegatives);
                _nestedExpressions.UnionWith(leftNestedExpressions);
            }
            return left;
        }

        if (!leftIsDifferentBinary)
        {
            _positives.UnionWith(leftPositives);
            _negatives.UnionWith(leftNegatives);
            _nestedExpressions.UnionWith(leftNestedExpressions);
        }
        if (!rightIsDifferentBinary)
        {
            _positives.UnionWith(rightPositives);
            _negatives.UnionWith(rightNegatives);
            _nestedExpressions.UnionWith(rightNestedExpressions);
        }

        if (_positives.Intersect(_negatives).Any())
        {
            _positives.Clear();
            _negatives.Clear();
            _nestedExpressions.Clear();
            return One;
        }

        return _positives.Select(name => (BooleanExpression)Literal(name))
            .Concat(_negatives.Select(name => Not(Literal(name))))
            .Concat(_nestedExpressions).Aggregate((e1, e2) => e1.Or(e2));
    }

    public override BooleanExpression RewriteLiteralExpression(LiteralExpression expression)
    {
        _positives.Add(expression.Name);
        return expression;
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
