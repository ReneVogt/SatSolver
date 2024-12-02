namespace Revo.SatSolver.Parsing.Expressions;

public sealed class LiteralExpression(string name) : BooleanExpression
{
    public string Name { get; } = name;
}
