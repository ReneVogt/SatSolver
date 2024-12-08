using FluentAssertions;
using Revo.SatSolver.Parsing;
using static Revo.SatSolver.BooleanAlgebra.RedundancyReducer;

namespace SatSolverTests.BooleanAlgebra;

public class RedundancyReducerTests
{
    [Fact]
    public void Reduce_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Reduce(null!));
    }

    [
        Theory,
        InlineData("0", "0"),
        InlineData("1", "1"),
        InlineData("!1", "0"),
        InlineData("!0", "1"),
        InlineData("a", "a"),
        InlineData("!!a", "a"),
        InlineData("a | b", "a | b"),
        InlineData("a & b", "a & b"),
        InlineData("a % b", "a % b"),
        InlineData("a | 0", "a"),
        InlineData("a | 1", "1"),
        InlineData("a & 0", "0"),
        InlineData("a & 1", "a"),
        InlineData("a % 1", "!a"),
        InlineData("a % 0", "a"),
        InlineData("a | a", "a"),
        InlineData("a | !a", "1"),
        InlineData("!a | a", "1"),
        InlineData("a & a", "a"),
        InlineData("a & !a", "0"),
        InlineData("!a & a", "0"),
        InlineData("a % a", "0"),
        InlineData("a % !a", "1"),
        InlineData("!a % a", "1"),
        InlineData("0 | 0", "0"),
        InlineData("0 | 1", "1"),
        InlineData("1 | 0", "1"),
        InlineData("1 | 1", "1"),
        InlineData("0 & 0", "0"),
        InlineData("0 & 1", "0"),
        InlineData("1 & 0", "0"),
        InlineData("1 & 1", "1"),
        InlineData("0 % 0", "0"),
        InlineData("0 % 1", "1"),
        InlineData("1 % 0", "1"),
        InlineData("1 % 1", "0"),

        InlineData("(a | b) % (a & c)", "(a | b) % a & c"),

        InlineData("a & (b | 1)", "a"),
        InlineData("(b & 0) | a", "a"),

        InlineData("a & (b | c | b)", "a & (b | c)"),
        InlineData("a | (b & c & b & d)", "a | b & c & d"),
        InlineData("a & b & c & !b & d", "0"),
        InlineData("a | b | c | !b | d", "1"),

        InlineData("a | !!(b & !b)", "a"),
        InlineData("a & b & (c | a | b | !c)", "a & b & c"),

        InlineData("(a | b) & (a | c) & (c | d) & (a | f) & (c | d | e) & f", "(a | b) & (a | c) & (c | d) & f"),
        InlineData("a & b | a & c | c & d | a & f | c & d & e | f", "a & b | a & c | a & f | c & d & e")
    ]
    public void Reduce_Correct(string input, string expected) => Reduce(BooleanAlgebraParser.Parse(input)).ToString().Should().Be(expected);
}
