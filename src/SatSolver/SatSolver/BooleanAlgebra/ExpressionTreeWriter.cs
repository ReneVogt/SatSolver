using System.Text;

namespace Revo.SatSolver.BooleanAlgebra;

public class ExpressionTreeWriter : BooleanExpressionRewriter
{
    readonly TextWriter? _writer;
    readonly StringBuilder? _builder;

    protected ExpressionTreeWriter(TextWriter? writer = null, StringBuilder? builder = null) =>
        (_writer, _builder) = (writer, builder);

    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        Write("(");
        Rewrite(expression.Left);
        Write(expression.Operator == BinaryOperator.Or 
            ? " | " 
            : expression.Operator == BinaryOperator.And 
                ? " & " 
                : $" {expression.Operator} ");
        Rewrite(expression.Right);
        Write(")");
        return expression;
    }
    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        Write(expression.Operator == UnaryOperator.Not ? "!" : expression.Operator.ToString());
        return base.RewriteUnaryExpression(expression);
    }
    public override BooleanExpression RewriteLiteralExpression(LiteralExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        Write(expression.Name);
        return base.RewriteLiteralExpression(expression);
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
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> or <paramref name="writer"/> is <c>null</c>.</exception>
    public static void Write(BooleanExpression expression, TextWriter writer) =>
        new ExpressionTreeWriter(writer: writer ?? throw new ArgumentNullException(nameof(writer))).Rewrite(expression);
    /// <summary>
    /// Writes the <paramref name="expression"/> to the <paramref name="builder"/>.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to write out.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to write to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> or <paramref name="builder"/> is <c>null</c>.</exception>
    public static void Write(BooleanExpression expression, StringBuilder builder) =>
        new ExpressionTreeWriter(builder: builder ?? throw new ArgumentNullException(nameof(builder))).Rewrite(expression);
}
