namespace Revo.SatSolver.Parsing.Expressions;

public sealed class UnaryExpression(UnaryOperator op, BooleanExpression expression) : BooleanExpression
{
    public UnaryOperator Operator { get; } = op;
    public BooleanExpression Expression { get; } = expression ?? throw new ArgumentNullException(nameof(expression));
}
