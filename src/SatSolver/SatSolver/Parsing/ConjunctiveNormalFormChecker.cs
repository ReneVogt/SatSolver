using Revo.BooleanAlgebra.Expressions;
using Revo.BooleanAlgebra.Transformers;

namespace Revo.SatSolver.Parsing;

/// <summary>
/// This determines if a  <see cref="BooleanExpression"/> is in a
/// conjunctive normal.
/// </summary>
sealed class ConjunctiveNormalFormChecker : BooleanExpressionRewriter
{
    bool _isCnf = true;
    bool _parentIsOr;

    ConjunctiveNormalFormChecker()
    {
    }

    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)
    {
        _isCnf = expression.Operator == UnaryOperator.Not && expression.Expression.Kind == ExpressionKind.Literal;
        return expression;
    }
    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression)
    {
        if (expression.Operator != BinaryOperator.Or && (expression.Operator != BinaryOperator.And || _parentIsOr))
        {
            _isCnf = false;
            return expression;
        }

        var old = _parentIsOr;
        _parentIsOr |= expression.Operator == BinaryOperator.Or;
        Rewrite(expression.Left);
        if (_isCnf) Rewrite(expression.Right);
        _parentIsOr = old;
        return expression;
    }
    public override BooleanExpression RewriteConstantExpression(ConstantExpression expression)
    {
        _isCnf = false;
        return expression;
    }

    /// <summary>
    /// Determines if a <see cref="BooleanExpression"/> is in
    /// a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to examine.</param>
    /// <returns><c>true</c> if the <paramref name="expression"/> is in a conjunctive normal form, <c>false</c> if not.
    /// that can be processed by the <see cref="SatSolver"/>.</returns>
    /// <exception cref="ArgumentNullException">The <paramref name="expression"/> is <c>null</c>.</exception>
    public static bool Check(BooleanExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        var transformer = new ConjunctiveNormalFormChecker();
        transformer.Rewrite(expression);
        return transformer._isCnf;
    }
}
