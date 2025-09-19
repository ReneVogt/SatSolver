using Revo.BooleanAlgebra.Expressions;
using Revo.BooleanAlgebra.Parsing;
using Revo.SatSolver.Parsing;

namespace SatSolverTests.Parsing;

public sealed class ConjunctiveNormalFormCheckerTests
{
    [Fact]
    public void Null_ArgumentNullException()
    {
        BooleanExpression? expression = null;
        Assert.Throws<ArgumentNullException>(() => expression!.IsConjunctiveNormalForm());
    }

    [Theory]
    [
        InlineData("1", false),
        InlineData("a", true),
        InlineData("a | b", true),
        InlineData("a & b", true),
        InlineData("a & (b | c)", true),
        InlineData("a | (b & c)", false),
        InlineData("a & (b | 1)", false),
        InlineData("(a | b) & c", true),
        InlineData("a & !(b | c)", false),
        InlineData("a & !b & !c", true),
        InlineData("(!a | !c) & !b", true),
        InlineData("!(a | b) & c", false),
        InlineData("a & !b & !c & !d", true),
        InlineData("(a | b) & (c | d | e) & (!d | e | !f) & !g & (!h | i)", true),
        InlineData("!h | i & b", false),
        InlineData("(a | b) & (c | d | e) & (!d | e | !f) & !g & (!h | i & b)", false),
        InlineData("(a | b) & (c | d | e) & (!d | e | !f) & !g & !(h | b)", false),
        InlineData("(a | b) & (c | d | e) & (!d | e | !f) & !g & !(h & b)", false),
        InlineData("a > b", false),
        InlineData("a < b", false),
        InlineData("a % b", false),
        InlineData("a | (b > c)", false),
        InlineData("b < c", false),
        InlineData("a | (b = c)", false),
        InlineData("a & (b = c)", false)
    ]
    public void Check_CorrectResults(string input, bool expected) => Assert.Equal(expected, BooleanAlgebraParser.Parse(input).IsConjunctiveNormalForm());

}
