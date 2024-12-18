using FluentAssertions;
using Revo.BooleanAlgebra.Parsing;
using static Revo.BooleanAlgebra.Expressions.ExpressionFactory;
using static Revo.SatSolver.Parsing.TseitinTransformer;

namespace SatSolverTests.Parsing;

public class TseitinTransformerTests
{
    [Fact]
    public void Transform_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Transform(null!));
    }

    [Fact]
    public void Transform_Literal_Same()
    {
        var expression = Literal("test");
        Assert.Same(expression, Transform(expression));
    }
    [Fact]
    public void Transform_DoubleNegated_Same()
    {
        var literal = Literal("test");
        var expression = Not(Not(literal));
        Assert.Same(literal, Transform(expression));
    }

    [Theory]
    [MemberData(nameof(ProvideTestCases))]
    public void Transform_CorrectTransformation(string input, string expected)
    {
        var expression = BooleanAlgebraParser.Parse(input);
        Transform(expression).ToString().Should().Be(expected);
    }

    public static TheoryData<string, string> ProvideTestCases() => new()
    {
        { "0", "0" },
        { "1", "1" },
        { "a | 0", "a" },
        { "a | 1", "1" },
        { "a & 0", "0" },
        { "a & 1", "a" },
        { "a | (b & 0 & c)", "a" },
        { "a & !(b | 1 | c)", "0" },
        { "a | b", ".t0 & (a | b | !.t0)" },
        { "a & b", ".t0 & (a | !.t0) & (b | !.t0)" },
        { "a % b", ".t2 & (a | b | !.t0) & (!.t1 | !a | !b) & (.t0 | !.t2) & (.t1 | !.t2)" },
        { "a = b", ".t2 & (b | !.t0 | !a) & (a | !.t1 | !b) & (.t0 | !.t2) & (.t1 | !.t2)" },
        { "!(a & b)", ".t0 & (!.t0 | !a | !b)" },
        { "!a | !b", ".t0 & (!.t0 | !a | !b)" },
        { "a | (b & c)", ".t1 & (b | !.t0) & (c | !.t0) & (a | .t0 | !.t1)" },
        { "a & (b | c)", ".t1 & (b | c | !.t0) & (a | !.t1) & (.t0 | !.t1)" },
        { "(a | b) & c", ".t1 & (a | b | !.t0) & (.t0 | !.t1) & (c | !.t1)" },
        { "(a & b) | c", ".t1 & (a | !.t0) & (b | !.t0) & (.t0 | c | !.t1)" },
        { "a & b & !c | a & !b & c | !a & b & c", ".t7 & (a | !.t0) & (b | !.t0) & (.t0 | !.t1) & (!.t1 | !c) & (a | !.t2) & (!.t2 | !b) & (.t2 | !.t3) & (c | !.t3) & (.t1 | .t3 | !.t4) & (!.t5 | !a) & (b | !.t5) & (.t5 | !.t6) & (c | !.t6) & (.t4 | .t6 | !.t7)" }
    };
}
