namespace Revo.SatSolver.BooleanAlgebra;

public class ExpressionTreeWriter : BooleanExpressionRewriter
{
    readonly TextWriter _writer;

    protected ExpressionTreeWriter(TextWriter writer) =>
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));

    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        _writer.Write('(');
        Rewrite(expression.Left);
        _writer.Write(expression.Operator == BinaryOperator.Or 
            ? " | " 
            : expression.Operator == BinaryOperator.And 
                ? " & " 
                : $" {expression.Operator} ");
        Rewrite(expression.Right);
        _writer.Write(')');
        return expression;
    }
    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        _writer.Write(expression.Operator == UnaryOperator.Not ? "!" : expression.Operator.ToString());
        return base.RewriteUnaryExpression(expression);
    }
    public override BooleanExpression RewriteLiteralExpression(LiteralExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        _writer.Write(expression.Name);
        return base.RewriteLiteralExpression(expression);
    }


    /// <summary>
    /// Writes the <paramref name="expression"/> to the <paramref name="writer"/> using indentation
    /// steps of <paramref name="indentation"/> whitespaces.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to write out.</param>
    /// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <c>null</c>.</exception>
    public static void Write(BooleanExpression expression, TextWriter writer) =>
        new ExpressionTreeWriter(writer).Rewrite(expression);
}
