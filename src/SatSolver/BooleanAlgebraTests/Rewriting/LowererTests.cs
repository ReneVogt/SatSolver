using FluentAssertions;
using Revo.BooleanAlgebra.Parsing;
using static Revo.BooleanAlgebra.Transformers.Lowerer;

namespace BooleanAlgebraTests.Rewriting;

public class LowererTests
{
    [Fact]
    public void Lower_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Lower(null!));
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
    public void Lower_IdentityForSimpleCases(string input)
    {
        var expression = BooleanAlgebraParser.Parse(input);
        Assert.Same(expression, Lower(expression));
    }

    [
        Theory,
        InlineData("a % b", "(a | b) & (!a | !b)"),
        InlineData("a & b | (c % !!d)", "a & b | (c | d) & (!c | !d)"),
        InlineData("a & b | c & 1", "a & b | c"),
        InlineData("a & b | c & 0", "a & b"),

        InlineData("a > b", "!a | b"),
        InlineData("a | b > c & d", "!(a | b) | c & d"),
        InlineData("a < b", "a | !b"),
        InlineData("a | b < c & d", "a | b | !(c & d)"),

        InlineData("a = b", "(!a | b) & (a | !b)"),
        InlineData("a | b = c & d", "(!(a | b) | c & d) & (a | b | !(c & d))")
    ]
    public void Lower_CorrectTransformation(string input, string expected)
    {
        var expression = BooleanAlgebraParser.Parse(input);
        Lower(expression).ToString().Should().Be(expected);
    }
}
