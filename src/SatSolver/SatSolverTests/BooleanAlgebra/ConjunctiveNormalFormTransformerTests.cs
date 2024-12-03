using Revo.SatSolver.BooleanAlgebra;
using Revo.SatSolver.Parsing;

using static Revo.SatSolver.BooleanAlgebra.ConjunctiveNormalFormTransformer;

namespace SatSolverTests.BooleanAlgebra;
public class ConjunctiveNormalFormTransformerTests()
{
    [Fact]
    public void Transform_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Transform(null!));
    }

    [Fact]
    public void Transform_Literal_Same()
    {
        var literal = new LiteralExpression("test");
        Assert.Same(literal, Transform(literal));
    }
    [Fact]
    public void Transform_SimpleOr_Same()
    {
        const string input = "a | b";
        var expression = BooleanAlgebraParser.Parse(input);
        Assert.Same(expression, Transform(expression));
    }
    [Fact]
    public void Transform_SimpleAnd_Same()
    {
        const string input = "a & b";
        var expression = BooleanAlgebraParser.Parse(input);
        Assert.Same(expression, Transform(expression));
    }
    [Fact]
    public void Transform_AndOfOr_Same()
    {
        const string input = "(a | b) & (b | c)";
        var expression = BooleanAlgebraParser.Parse(input);
        Assert.Same(expression, Transform(expression));
    }
    [Fact]
    public void Transform_OrOfAnd_Transformed()
    {
        const string input = "a | b & c";
        var expression = BooleanAlgebraParser.Parse(input);
        var transformed = Transform(expression);
        using var e = new BooleanExpressionAsserter(transformed);

        e.AssertAnd();
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("b");
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("c");
    }
}
