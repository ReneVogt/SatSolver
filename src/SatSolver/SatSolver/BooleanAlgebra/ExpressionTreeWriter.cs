using System.Text;
using static Revo.SatSolver.BooleanAlgebra.BooleanExpressionException;

namespace Revo.SatSolver.BooleanAlgebra;

public class ExpressionTreeWriter : BooleanExpressionRewriter
{
    readonly TextWriter? _writer;
    readonly StringBuilder? _builder;
    readonly bool _explicitParentheses;

    int _parentPrecedence;

    protected ExpressionTreeWriter(bool explicitParentheses, TextWriter? writer = null, StringBuilder? builder = null) =>
        (_explicitParentheses, _writer, _builder) = (explicitParentheses, writer, builder);

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

        if (parentheses) Write("(");
        Rewrite(expression.Left);
        Write(expression.Operator switch
        {
            BinaryOperator.Or => " | ",
            BinaryOperator.And => " & ",
            BinaryOperator.Xor => " % ",
            _ => throw UnsupportedBinaryOperator(expression.Operator)
        });
        Rewrite(expression.Right);
        if (parentheses) Write(")");
        return expression;
    }
    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        _parentPrecedence = expression.Operator.GetPrecedence();
        Write(expression.Operator == UnaryOperator.Not ? "!" : throw UnsupportedUnaryOperator(expression.Operator));
        return base.RewriteUnaryExpression(expression);
    }
    public override BooleanExpression RewriteLiteralExpression(LiteralExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        Write(expression.Name);
        return base.RewriteLiteralExpression(expression);
    }
    public override BooleanExpression RewriteConstantExpression(ConstantExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        Write(expression.Sense ? "1" : "0");
        return base.RewriteConstantExpression(expression);
    }

    void Write(string text)
    {
        _writer?.Write(text);
        _builder?.Append(text);
    }

    /// <summary>
    /// Writes the <paramref name="expression"/> to the <paramref name="writer"/>.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to write out.</param>
    /// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
    /// <param name="explicitParentheses">If <c>true</c> explicit parentheses are written around every binary expression (except root).</param>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> or <paramref name="writer"/> is <c>null</c>.</exception>
    public static void Write(BooleanExpression expression, TextWriter writer, bool explicitParentheses = false) =>
        new ExpressionTreeWriter(explicitParentheses, writer: writer ?? throw new ArgumentNullException(nameof(writer))).Rewrite(expression);

    /// <summary>
    /// Writes the <paramref name="expression"/> to the <paramref name="builder"/>.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to write out.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to write to.</param>
    /// <param name="explicitParentheses">If <c>true</c> explicit parentheses are written around every binary expression (except root).</param>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> or <paramref name="builder"/> is <c>null</c>.</exception>
    public static void Write(BooleanExpression expression, StringBuilder builder, bool explicitParentheses = false) =>
        new ExpressionTreeWriter(explicitParentheses, builder: builder ?? throw new ArgumentNullException(nameof(builder))).Rewrite(expression);
}
