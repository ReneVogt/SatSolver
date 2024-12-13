using Revo.BooleanAlgebra.Expressions;
using static Revo.BooleanAlgebra.Expressions.BooleanExpressionException;

namespace Revo.BooleanAlgebra.Transformers;

/// <summary>
/// Abstract base class for <see cref="BooleanExpression"/> tree
/// rewriters.
/// </summary>
public abstract class BooleanExpressionRewriter
{
    protected BooleanExpressionRewriter()
    {
    }

    /// <summary>
    /// Visits a <see cref="BooleanExpression"/> in a tree (or the root expression)
    /// to rewrite the tree.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to rewrite.</param>
    /// <returns>A rewritten <see cref="BooleanExpression"/> or the original <paramref name="expression"/> if
    /// no changes were made.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> ís <c>null</c>.</exception>
    public virtual BooleanExpression Rewrite(BooleanExpression expression) =>
        (expression?.Kind ?? throw new ArgumentNullException(nameof(expression))) switch
        {
            ExpressionKind.Binary => RewriteBinaryExpression((BinaryExpression)expression),
            ExpressionKind.Unary => RewriteUnaryExpression((UnaryExpression)expression),
            ExpressionKind.Literal => RewriteLiteralExpression((LiteralExpression)expression),
            ExpressionKind.Constant => RewriteConstantExpression((ConstantExpression)expression),
            _ => throw UnsupportedExpressionKind(expression.Kind)
        };

    /// <summary>
    /// Visits a <see cref="BinaryExpression"/> and dispatches calls for its children.
    /// In a derived class rewrites the expression as necessary.
    /// </summary>
    /// <param name="expression">The <see cref="BinaryExpression"/> to visit.</param>
    /// <returns>A rewritten <see cref="BooleanExpression"/> or the original <paramref name="expression"/> if
    /// no changes were made.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> ís <c>null</c>.</exception>
    public virtual BooleanExpression RewriteBinaryExpression(BinaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        var left = Rewrite(expression.Left);
        var right = Rewrite(expression.Right);

        return left == expression.Left && right == expression.Right
            ? expression
            : new BinaryExpression(left, expression.Operator, right);
    }

    /// <summary>
    /// Visits a <see cref="UnaryExpression"/> and dispatches calls for its children.
    /// In a derived class rewrites the expression as necessary.
    /// </summary>
    /// <param name="expression">The <see cref="UnaryExpression"/> to visit.</param>
    /// <returns>A rewritten <see cref="BooleanExpression"/> or the original <paramref name="expression"/> if
    /// no changes were made.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> ís <c>null</c>.</exception>
    public virtual BooleanExpression RewriteUnaryExpression(UnaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        var exp = Rewrite(expression.Expression);

        return exp == expression.Expression
            ? expression
            : new UnaryExpression(expression.Operator, exp);
    }

    /// <summary>
    /// Visits a <see cref="LiteralExpression"/>.
    /// In a derived class rewrites the expression as necessary.
    /// </summary>
    /// <param name="expression">The <see cref="LiteralExpression"/> to visit.</param>
    /// <returns>A rewritten <see cref="BooleanExpression"/> or the original <paramref name="expression"/> if
    /// no changes were made.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> ís <c>null</c>.</exception>
    public virtual BooleanExpression RewriteLiteralExpression(LiteralExpression expression) => expression ?? throw new ArgumentNullException(nameof(expression));

    /// <summary>
    /// Visits a <see cref="ConstantExpression"/>.
    /// In a derived class rewrites the expression as necessary.
    /// </summary>
    /// <param name="expression">The <see cref="ConstantExpression"/> to visit.</param>
    /// <returns>A rewritten <see cref="BooleanExpression"/> or the original <paramref name="expression"/> if
    /// no changes were made.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> ís <c>null</c>.</exception>
    public virtual BooleanExpression RewriteConstantExpression(ConstantExpression expression) => expression ?? throw new ArgumentNullException(nameof(expression));
}
