using Revo.BooleanAlgebra.Expressions;
using System.Text;

namespace Revo.BooleanAlgebra.Transformers;

public class ExpressionTreeWriter : BooleanExpressionRewriter
{
    readonly StringBuilder _builder = new();
    readonly bool _explicitParentheses;

    int _parentPrecedence;

    protected ExpressionTreeWriter(bool explicitParentheses) =>
        _explicitParentheses = explicitParentheses;

    public override BooleanExpression Rewrite(BooleanExpression expression)
    {
        var savedPrecedence = _parentPrecedence;
        try
        {
            return base.Rewrite(expression);
        }
        finally
        {
            _parentPrecedence = savedPrecedence;
        }
    }
    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));

        var precedence = expression.Operator.GetPrecedence();
        var parentheses = _parentPrecedence > 0 && (_explicitParentheses || precedence < _parentPrecedence);
        _parentPrecedence = precedence;

        if (parentheses) _builder.Append('(');
        Rewrite(expression.Left);
        _builder.Append(' ');
        _builder.Append(expression.Operator.GetOperatorChar());
        _builder.Append(' ');
        Rewrite(expression.Right);
        if (parentheses) _builder.Append(')');
        return expression;
    }
    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        _parentPrecedence = expression.Operator.GetPrecedence();
        _builder.Append(expression.Operator.GetOperatorChar());
        return base.RewriteUnaryExpression(expression);
    }
    public override BooleanExpression RewriteLiteralExpression(LiteralExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        _builder.Append(expression.Name);
        return base.RewriteLiteralExpression(expression);
    }
    public override BooleanExpression RewriteConstantExpression(ConstantExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        _builder.Append(expression.Sense ? '1' : '0');
        return base.RewriteConstantExpression(expression);
    }

    /// <summary>
    /// Represents the <paramref name="expression"/> as a string.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to write out.</param>
    /// <returns>The string representation of the given <paramref name="expression"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <c>null</c>.</exception>
    public static string Write(BooleanExpression expression, bool explicitParentheses = false)
    {
        var writer = new ExpressionTreeWriter(explicitParentheses);
        writer.Rewrite(expression);
        return writer._builder.ToString();
    }
}
