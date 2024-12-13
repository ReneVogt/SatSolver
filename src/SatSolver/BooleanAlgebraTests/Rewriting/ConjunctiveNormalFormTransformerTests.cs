using FluentAssertions;
using Revo.BooleanAlgebra.Parsing;
using static Revo.BooleanAlgebra.Transformers.ConjunctiveNormalFormTransformer;

namespace BooleanAlgebraTests.Rewriting;

public class ConjunctiveNormalFormTransformerTests
{
    [Fact]
    public void Transform_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Transform(null!));
    }

    [
        Theory,
        InlineData("test"),
        InlineData("a | b"),
        InlineData("a & b"),
        InlineData("a & (b | c)"),
        InlineData("(a | b) & c"),
        InlineData("(a | b) & (b | c)")
    ]
    public void Transform_IdentityForSimpleCases(string input)
    {
        var expression = BooleanAlgebraParser.Parse(input);
        Assert.Same(expression, Transform(expression));
    }

    [
        Theory,
        InlineData("a | b & c", "(a | b) & (a | c)"),
        InlineData("a & b | c", "(a | c) & (b | c)"),
        InlineData("a & b | c & d", "(a | c) & (a | d) & (b | c) & (b | d)"),
        InlineData("a % b", "(a | b) & (!a | !b)"),
        InlineData("a & !(b | c) | !(a & b)", "(a | !a | !b) & (!b | !a | !b) & (!c | !a | !b)"),
        InlineData("a & !b | !a & b", "(a | !a) & (a | b) & (!b | !a) & (!b | b)"),
        InlineData("a & (b % c)", "a & (b | c) & (!b | !c)"),
        InlineData("a % b | c", "(a | b | c) & (!a | !b | c)"),
        // 2o3
        InlineData("a & b & !c | a & !b & c | !a & b & c", "(a | a | !a) & (a | !b | !a) & (b | a | !a) & (b | !b | !a) & (a | c | !a) & (b | c | !a) & (a | a | b) & (a | !b | b) & (b | a | b) & (b | !b | b) & (a | c | b) & (b | c | b) & (!c | a | !a) & (!c | !b | !a) & (!c | a | b) & (!c | !b | b) & (a | a | c) & (a | !b | c) & (b | a | c) & (b | !b | c) & (a | c | c) & (b | c | c) & (!c | a | c) & (!c | !b | c) & (!c | c | !a) & (!c | c | b) & (!c | c | c)")
    ]
    public void Transform_CorrectTransformation(string input, string expected)
    {
        var expression = BooleanAlgebraParser.Parse(input);
        expression.ToString().Should().Be(input);
        Transform(expression).ToString().Should().Be(expected);
    }
}
