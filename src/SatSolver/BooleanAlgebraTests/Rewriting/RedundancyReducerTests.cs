using FluentAssertions;
using Revo.BooleanAlgebra.Parsing;
using static Revo.BooleanAlgebra.Transformers.RedundancyReducer;

namespace BooleanAlgebraTests.Rewriting;

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
        InlineData("a & b & (c | a | b | !c)", "a & b"),

        InlineData("a & (b | c & e)", "a & (b | c & e)"),

        InlineData("a & (b | c) & (b | c | d | e & f) & (b | c | d) & g", "a & (b | c) & g"),
        InlineData("a | (b & c & d) | (b & c & (d | f)) | (b & c) | g", "a | b & c & d | b & c & (d | f) | b & c | g"),
        InlineData("extra | (a & (b | c) & (b | c | d | e & f) & (b | c | d) & g) | suffix", "extra | a & (b | c) & g | suffix"),
        InlineData("extra & (a | (b & c & d) | (b & c & (d | f)) | (b & c) | g) & suffix", "extra & (a | b & c & d | b & c & (d | f) | b & c | g) & suffix"),

        InlineData("(a | b) & (a | c) & (c | d) & (a | f) & (c | d | e) & f", "(a | b) & (a | c) & (c | d) & f"),
        InlineData("a & b | a & c | c & d | a & f | c & d & e | f", "a & b | a & c | c & d | a & f | c & d & e | f"),

        InlineData("a & b & !c | a & !b & c | !a & b | c", "a & b & !c | a & !b & c | !a & b | c"),
        InlineData("a & b & !c | a & !b & c | !a & b & c", "a & b & !c | a & !b & c | !a & b & c"),

        InlineData("t & (!t | !a | !b) & (t | a) & (t | b)", "t & (!t | !a | !b)"),

        // The CNF result of 2o3
        InlineData("(a | a | !a) & (a | !b | !a) & (b | a | !a) & (b | !b | !a) & (a | c | !a) & (b | c | !a) & (a | a | b) & (a | !b | b) & (b | a | b) & (b | !b | b) & (a | c | b) & (b | c | b) & (!c | a | !a) & (!c | !b | !a) & (!c | a | b) & (!c | !b | b) & (a | a | c) & (a | !b | c) & (b | a | c) & (b | !b | c) & (a | c | c) & (b | c | c) & (!c | a | c) & (!c | !b | c) & (!c | c | !a) & (!c | c | b) & (!c | c | c)", "(a | b) & (b | c) & (!c | !b | !a) & (a | c)"),
        InlineData("(((((((((((a | a) | !a) & ((a | !b) | !a)) & ((b | a) | !a)) & ((b | !b) | !a)) & (((a | c) | !a) & ((b | c) | !a))) & ((((((a | a) | b) & ((a | !b) | b)) & ((b | a) | b)) & ((b | !b) | b)) & (((a | c) | b) & ((b | c) | b)))) & (((!c | a) | !a) & ((!c | !b) | !a))) & (((!c | a) | b) & ((!c | !b) | b))) & (((((((a | a) | c) & ((a | !b) | c)) & ((b | a) | c)) & ((b | !b) | c)) & (((a | c) | c) & ((b | c) | c))) & (((!c | a) | c) & ((!c | !b) | c)))) & (((!c | c) | !a) & ((!c | c) | b))) & ((!c | c) | c)", "(a | b) & (b | c) & (!c | !b | !a) & (a | c)")
    ]
    public void Reduce_Correct(string input, string expected) => Reduce(BooleanAlgebraParser.Parse(input)).ToString().Should().Be(expected);

    [Fact]
    public void Reduce_Test() => Reduce_Correct("(((a | b) & ((a | b) | c) | d) & f)", "(a | b | d) & f");
}
