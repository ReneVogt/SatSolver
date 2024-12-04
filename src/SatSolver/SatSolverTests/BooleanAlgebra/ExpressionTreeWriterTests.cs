using Revo.SatSolver.BooleanAlgebra;
using static Revo.SatSolver.BooleanAlgebra.ExpressionFactory;

namespace SatSolverTests.BooleanAlgebra;
public class ExpressionTreeWriterTests()
{
    [Fact]
    public void Write_Literal()
    {
        var expression = new LiteralExpression("test");
        using var writer = new StringWriter();
        ExpressionTreeWriter.Write(expression, writer);
        Assert.Equal("test", writer.ToString());
    }
    [Fact]
    public void Write_NegatedLiteral()
    {
        var expression = new UnaryExpression(UnaryOperator.Not, new LiteralExpression("test"));
        using var writer = new StringWriter();
        ExpressionTreeWriter.Write(expression, writer);
        Assert.Equal("!test", writer.ToString());
    }
    [Fact]
    public void Write_Or()
    {
        var expression = new BinaryExpression(
            new LiteralExpression("left"),
            BinaryOperator.Or, 
            new LiteralExpression("right"));
        using var writer = new StringWriter();
        ExpressionTreeWriter.Write(expression, writer);
        Assert.Equal("(left | right)", writer.ToString());
    }
    [Fact]
    public void Write_And()
    {
        var expression = new BinaryExpression(
            new LiteralExpression("left"),
            BinaryOperator.And,
            new LiteralExpression("right"));
        using var writer = new StringWriter();
        ExpressionTreeWriter.Write(expression, writer);
        Assert.Equal("(left & right)", writer.ToString());
    }
    [Fact]
    public void Write_Complex()
    {
        var expression = Not(Not(Literal("lit1"))).Or(
            Not(Literal("lit2").Or(Literal("lit3")))
            .And(Literal("lit4").Or(Not(Literal("lit5")))));
        using var writer = new StringWriter();
        ExpressionTreeWriter.Write(expression, writer);

        Assert.Equal("(!!lit1 | (!(lit2 | lit3) & (lit4 | !lit5)))", writer.ToString());
    }
}
