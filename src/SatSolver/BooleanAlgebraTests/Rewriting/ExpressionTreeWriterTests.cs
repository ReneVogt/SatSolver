using FluentAssertions;
using Revo.BooleanAlgebra.Expressions;
using Revo.BooleanAlgebra.Parsing;
using Revo.BooleanAlgebra.Transformers;
using System.Globalization;
using static Revo.BooleanAlgebra.Expressions.ExpressionFactory;

namespace BooleanAlgebraTests.Rewriting;
public class ExpressionTreeWriterTests()
{
    [Fact]
    public void Write_Literal()
    {
        var expression = new LiteralExpression("test"); ;
        Assert.Equal("test", ExpressionTreeWriter.Write(expression));
    }
    [Fact]
    public void Write_NegatedLiteral()
    {
        var expression = new UnaryExpression(UnaryOperator.Not, new LiteralExpression("test"));
        Assert.Equal("!test", ExpressionTreeWriter.Write(expression));
    }
    [Fact]
    public void Write_Or()
    {
        var expression = new BinaryExpression(
            new LiteralExpression("left"),
            BinaryOperator.Or,
            new LiteralExpression("right"));
        Assert.Equal("left | right", ExpressionTreeWriter.Write(expression));
    }
    [Fact]
    public void Write_And()
    {
        var expression = new BinaryExpression(
            new LiteralExpression("left"),
            BinaryOperator.And,
            new LiteralExpression("right"));
        Assert.Equal("left & right", ExpressionTreeWriter.Write(expression));
    }
    [Fact]
    public void Write_Complex()
    {
        var expression = Not(Not(Literal("lit1"))).Or(
            Not(Literal("lit2").Or(Literal("lit3")))
            .And(Literal("lit4").Or(Not(Literal("lit5")))));
        Assert.Equal("!!lit1 | !(lit2 | lit3) & (lit4 | !lit5)", ExpressionTreeWriter.Write(expression));
    }

    [
        Theory,
        InlineData("a", "a", "a"),
        InlineData("a | b", "a | b", "a | b"),
        InlineData("a & b", "a & b", "a & b"),
        InlineData("a % b", "a % b", "a % b"),
        InlineData("a = b", "a = b", "a = b"),
        InlineData("a < b", "a < b", "a < b"),
        InlineData("a > b", "a > b", "a > b"),
        InlineData("!(a | b & c % (d | a))", "!(a | b & c % (d | a))", "!(a | ((b & c) % (d | a)))")
    ]
    public void Write_Formatted(string input, string withoutParentheses, string withParentheses)
    {
        var expression = BooleanAlgebraParser.Parse(input);
        expression.ToString().Should().Be(withoutParentheses);
        expression.ToString("N", CultureInfo.InvariantCulture).Should().Be(withoutParentheses);
        expression.ToString("P", CultureInfo.InvariantCulture).Should().Be(withParentheses);
    }
}
