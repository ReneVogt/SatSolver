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

    [Fact]
    public void Transform_OrOfAnds_Transformed()
    {
        const string input = "a & b | c & d";
        var expression = BooleanAlgebraParser.Parse(input);
        expression.ToString().Should().Be("((a & b) | (c & d))");
        var transformed = Transform(expression);
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
        transformed.ToString().Should().Be("(((!a | !b) | a) & (((!a | !b) | !b) & ((!a | !b) | !c)))");

        using var e = new BooleanExpressionAsserter(transformed);
        e.AssertAnd();

        e.AssertOr();
        e.AssertOr();
        e.AssertNot();
        e.AssertLiteral("a");
        e.AssertNot();
        e.AssertLiteral("b");
        e.AssertLiteral("a");

        e.AssertAnd();
        e.AssertOr();
        e.AssertOr();
        e.AssertNot();
        e.AssertLiteral("a");
        e.AssertNot();
        e.AssertLiteral("b");
        e.AssertNot();
        e.AssertLiteral("b");

        e.AssertOr();
        e.AssertOr();
        e.AssertNot();
        e.AssertLiteral("a");
        e.AssertNot();
        e.AssertLiteral("b");
        e.AssertNot();
        e.AssertLiteral("c");
    }


    [Fact]
    public void Transform_2of3_Transformed()
    {
        const string input = "a & b & !c | a&!b&c | !a&b&c";
        var expression = BooleanAlgebraParser.Parse(input);
        var transformed = Transform(expression);
        transformed.ToString().Should().Be("(((((((!a | (a | a)) & (!a | (a | !b))) & ((!a | (b | a)) & (!a | (b | !b)))) & (((b | (a | a)) & (b | (a | !b))) & ((b | (b | a)) & (b | (b | !b))))) & (((!a | (c | a)) & (!a | (c | b))) & ((b | (c | a)) & (b | (c | b))))) & ((((c | (a | a)) & (c | (a | !b))) & ((c | (b | a)) & (c | (b | !b)))) & ((c | (c | a)) & (c | (c | b))))) & (((((!a | (!c | a)) & (!a | (!c | !b))) & ((b | (!c | a)) & (b | (!c | !b)))) & (((!c | c) | !a) & ((!c | c) | b))) & (((c | (!c | a)) & (c | (!c | !b))) & (c | (!c | c)))))");
        Assert.Fail("Boolean expressions should be reduced after converting to CNF. Remove redundand literals and trivially true clauses.");
    }
}
