using FluentAssertions;
using Revo.SatSolver.BooleanAlgebra;
using Revo.SatSolver.Parsing;
using Xunit.Abstractions;
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
        transformed.ToString().Should().Be("((c | a) & (c | b))");

        using var e = new BooleanExpressionAsserter(transformed);

        e.AssertAnd();
        e.AssertOr();
        e.AssertLiteral("c");
        e.AssertLiteral("a");
        e.AssertOr();
        e.AssertLiteral("c");
        e.AssertLiteral("b");
    }

    [Fact]
    public void Transform_OrOfAnds_Transformed()
    {
        const string input = "a & b | c & d";
        var expression = BooleanAlgebraParser.Parse(input);
        expression.ToString().Should().Be("((a & b) | (c & d))");
        var transformed = Transform(expression);
        Assert.NotNull(transformed);
        using var e = new BooleanExpressionAsserter(transformed);
        e.AssertAnd();

        e.AssertAnd();
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("c");

        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("d");

        e.AssertAnd();
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
        transformed.ToString().Should().Be("(!c | (!a | !b))");

        using var e = new BooleanExpressionAsserter(transformed);
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
    public void Transform_DistributedRedundancyRight_Removed()
    {
        var expression = BooleanAlgebraParser.Parse("a | (!a & b) | c");
        expression.ToString().Should().Be("((a | (!a & b)) | c)");
        var transformed = Transform(expression);
        Assert.NotNull(transformed);
        transformed.ToString().Should().Be("(a | (b | c))");

        using var e = new BooleanExpressionAsserter(transformed);
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertOr();
        e.AssertLiteral("b");
        e.AssertLiteral("c");
    }
    [Fact]
    public void Transform_DistributedRedundancyLeft_Removed()
    {
        var expression = BooleanAlgebraParser.Parse("(!a & b) | c | a");
        expression.ToString().Should().Be("(((!a & b) | c) | a)");
        var transformed = Transform(expression);
        Assert.NotNull(transformed);
        transformed.ToString().Should().Be("(a | (c | b))");

        using var e = new BooleanExpressionAsserter(transformed);
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertOr();
        e.AssertLiteral("c");
        e.AssertLiteral("b");
    }
    [Fact]
    public void Transform_XOR_Transformed()
    {
        var expression = BooleanAlgebraParser.Parse("a & !b | !a & b");
        expression.ToString().Should().Be("((a & !b) | (!a & b))");
        var transformed = Transform(expression);
        Assert.NotNull(transformed);
        transformed.ToString().Should().Be("((a | b) & (!b | !a))");

        using var e = new BooleanExpressionAsserter(transformed);
        e.AssertAnd();
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("b");
        e.AssertOr();
        e.AssertNot();
        e.AssertLiteral("b");
        e.AssertNot();
        e.AssertLiteral("a");
    }

    [Fact]
    public void Transform_2of3_Transformed()
    {
        const string input = "a & b & !c | a&!b&c | !a&b&c";
        var expression = BooleanAlgebraParser.Parse(input);
        var transformed = Transform(expression);
        Assert.NotNull(transformed);
        transformed.ToString().Should().Be("");
    }

    [Fact]
    public void Transform_PartiallyAlwaysTrue()
    {
        const string input = "a & (b | c | !b) & (c | d)";
        var expression = BooleanAlgebraParser.Parse(input);
        var transformed = Transform(expression);
        Assert.NotNull(transformed);
        transformed.ToString().Should().Be("(a & (c | d))");
        using var e = new BooleanExpressionAsserter(transformed);
        e.AssertAnd();
        e.AssertLiteral("a");
        e.AssertOr();
        e.AssertLiteral("c");
        e.AssertLiteral("d");

    }
    [Fact]
    public void Transform_AlwaysTrue_Null()
    {
        const string input = "((a | c) | !a) & (b | c | !b)";
        var expression = BooleanAlgebraParser.Parse(input);
        Transform(expression).Should().BeNull();
    }

}
