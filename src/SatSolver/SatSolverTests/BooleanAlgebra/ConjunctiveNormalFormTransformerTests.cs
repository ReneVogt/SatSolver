using FluentAssertions;
using Revo.SatSolver.BooleanAlgebra;
using Revo.SatSolver.Parsing;
using static Revo.SatSolver.BooleanAlgebra.ConjunctiveNormalFormTransformer;

namespace SatSolverTests.BooleanAlgebra;

public class ConjunctiveNormalFormTransformerTests
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
        Transform(expression)?.ToString().Should().Be(expression.ToString());
    }
    [Fact]
    public void Transform_SimpleAnd_Same()
    {
        const string input = "a & b";
        var expression = BooleanAlgebraParser.Parse(input);
        Transform(expression)?.ToString().Should().Be(expression.ToString());
    }
    [Fact]
    public void Transform_AndOfOrLeft_Same()
    {
        const string input = "a & (b | c)";
        var expression = BooleanAlgebraParser.Parse(input);
        Transform(expression)?.ToString().Should().Be(expression.ToString());
    }
    [Fact]
    public void Transform_AndOfOrRight_Same()
    {
        const string input = "(a | b) & c";
        var expression = BooleanAlgebraParser.Parse(input);
        Transform(expression)?.ToString().Should().Be(expression.ToString());
    }
    [Fact]
    public void Transform_AndOfOrs_Same()
    {
        const string input = "(a | b) & (b | c)";
        var expression = BooleanAlgebraParser.Parse(input);
        Transform(expression)?.ToString().Should().Be(expression.ToString());
    }
    [Fact]
    public void Transform_OrOfAndLeft_Transformed()
    {
        const string input = "a | b & c";
        var expression = BooleanAlgebraParser.Parse(input);
        var transformed = Transform(expression);
        Assert.NotNull(transformed);
        using var e = new BooleanExpressionAsserter(transformed);

        e.AssertAnd();
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("b");
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("c");
    }
    [Fact]
    public void Transform_OrOfAndRight_Transformed()
    {
        const string input = "a & b | c";
        var expression = BooleanAlgebraParser.Parse(input);
        var transformed = Transform(expression);
        Assert.NotNull(transformed);
        transformed.ToString().Should().Be("((a | c) & (b | c))");

        using var e = new BooleanExpressionAsserter(transformed);

        e.AssertAnd();
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("c");
        e.AssertOr();
        e.AssertLiteral("b");
        e.AssertLiteral("c");
    }

    [Fact]
    public void Transform_OrOfAnds_Transformed()
    {
        const string input = "a & b | c & d";
        var expression = BooleanAlgebraParser.Parse(input);
        expression.ToString().Should().Be("((a & b) | (c & d))");
        var transformed = Transform(expression);
        transformed.ToString().Should().Be("((((a | c) & (a | d)) & (b | c)) & (b | d))");
        Assert.NotNull(transformed);
        using var e = new BooleanExpressionAsserter(transformed);
        e.AssertAnd();
        e.AssertAnd();
        e.AssertAnd();
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("c");
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("d");
        e.AssertOr();
        e.AssertLiteral("b");
        e.AssertLiteral("c");
        e.AssertOr();
        e.AssertLiteral("b");
        e.AssertLiteral("d");
    }

    [Fact]
    public void Transform_NestedNegation_Transformed()
    {
        const string input = "a & !(b | c) | !(a & b)";
        var expression = BooleanAlgebraParser.Parse(input);
        expression.ToString().Should().Be("((a & !(b | c)) | !(a & b))");
        var transformed = Transform(expression);
        Assert.NotNull(transformed);
        transformed.ToString().Should().Be("((a | (!a | !b)) & ((!b | (!a | !b)) & (!c | (!a | !b))))");

        using var e = new BooleanExpressionAsserter(transformed);
        e.AssertAnd();
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertOr();
        e.AssertNot();
        e.AssertLiteral("a");
        e.AssertNot();
        e.AssertLiteral("b");
        e.AssertAnd();
        e.AssertOr();
        e.AssertNot();
        e.AssertLiteral("b");
        e.AssertOr();
        e.AssertNot();
        e.AssertLiteral("a");
        e.AssertNot();
        e.AssertLiteral("b");
        e.AssertOr();
        e.AssertNot();
        e.AssertLiteral("c");
        e.AssertOr();
        e.AssertNot();
        e.AssertLiteral("a");
        e.AssertNot();
        e.AssertLiteral("b");
    }

    [Fact]
    public void Transform_XOR_Transformed()
    {
        var expression = BooleanAlgebraParser.Parse("a & !b | !a & b");
        expression.ToString().Should().Be("((a & !b) | (!a & b))");
        var transformed = Transform(expression);
        Assert.NotNull(transformed);
        transformed.ToString().Should().Be("((((a | !a) & (a | b)) & (!b | !a)) & (!b | b))");

        using var e = new BooleanExpressionAsserter(transformed);
        e.AssertAnd();
        e.AssertAnd();
        e.AssertAnd();
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertNot();
        e.AssertLiteral("a");
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("b");
        e.AssertOr();
        e.AssertNot();
        e.AssertLiteral("b");
        e.AssertNot();
        e.AssertLiteral("a");
        e.AssertOr();
        e.AssertNot();
        e.AssertLiteral("b");
        e.AssertLiteral("b");

    }

    [Fact]
    public void Transform_2of3_Transformed()
    {
        const string input = "a & b & !c | a&!b&c | !a&b&c";
        var expression = BooleanAlgebraParser.Parse(input);
        var transformed = Transform(expression);
        Assert.NotNull(transformed);
        transformed.ToString().Should().Be("((((((((((((a | a) | !a) & ((a | !b) | !a)) & ((b | a) | !a)) & ((b | !b) | !a)) & (((a | c) | !a) & ((b | c) | !a))) & ((((((a | a) | b) & ((a | !b) | b)) & ((b | a) | b)) & ((b | !b) | b)) & (((a | c) | b) & ((b | c) | b)))) & (((!c | a) | !a) & ((!c | !b) | !a))) & (((!c | a) | b) & ((!c | !b) | b))) & (((((((a | a) | c) & ((a | !b) | c)) & ((b | a) | c)) & ((b | !b) | c)) & (((a | c) | c) & ((b | c) | c))) & (((!c | a) | c) & ((!c | !b) | c)))) & (((!c | c) | !a) & ((!c | c) | b))) & ((!c | c) | c))");
        Assert.Fail("Just to remember to forward this test to the redundancy reducer.");
    }
}
