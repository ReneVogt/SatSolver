namespace Revo.SatSolver.Parsing.Expressions;

public sealed class BinaryExpression(BooleanExpression left, BinaryOperator op, BooleanExpression right) : BooleanExpression
{
    public BooleanExpression Left { get; } = left ?? throw new ArgumentNullException(nameof(left));
    public BinaryOperator Operator { get; } = op;
    public BooleanExpression Right { get; } = right ?? throw new ArgumentNullException(nameof(right));
}
