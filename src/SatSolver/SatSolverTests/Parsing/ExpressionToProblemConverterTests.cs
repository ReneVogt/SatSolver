using FluentAssertions;
using Revo.BooleanAlgebra.Parsing;
using Revo.SatSolver.Parsing;

namespace SatSolverTests.Parsing;

public class ExpressionToProblemConverterTests
{
    [Fact]
    public void Convert_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ExpressionToProblemConverter.ToProblem(null!));
    }

    [Theory]
    [MemberData(nameof(ProvideRawCases))]
    public void ToProblem_Raw_Correct(string input, string expected, string[] literals)
    {
        BooleanAlgebraParser.Parse(input).ToProblem(literalMapping: out var mapping).ToString().Should().Be(expected);
        mapping.Count.Should().Be(literals.Length);
        for (var i = 0; i < literals.Length; i++)
            mapping[literals[i]].Should().Be(i+1);
    }
    [Theory]
    [MemberData(nameof(ProvideTransformedCases))]
    public void ToProblem_Transformed_Correct(string input, string expected, string[] literals)
    {
        var expression = BooleanAlgebraParser.Parse(input);
        ExpressionToProblemConverter.ToProblemInternal(expression, literalMapping: out var mapping).ToString().Should().Be(expected);
        mapping.Count.Should().Be(literals.Length);
        for (var i = 0; i < literals.Length; i++)
            mapping[literals[i]].Should().Be(i+1);
    }

    public static TheoryData<string, string, string[]> ProvideTransformedCases()
    {
        var data = new TheoryData<string, string, string[]>
        {
            { "0", "p cnf 0 1\r\n0", [] },
            { "1", "p cnf 0 0\r\n", [] },
            { "a", "p cnf 1 1\r\n1 0", ["a"] },
            { "a | b", "p cnf 2 1\r\n1 2 0", ["a", "b"] },
            { "a | b | c", "p cnf 3 1\r\n1 2 3 0", ["a", "b", "c"] },
            { "a & b", "p cnf 2 2\r\n1 0\r\n2 0", ["a", "b"] },
            { "(a | b) & (!a | c) & (b | !c)", "p cnf 3 3\r\n-1 3 0\r\n1 2 0\r\n2 -3 0", ["a", "b", "c"] },
            { "!a | !b", "p cnf 2 1\r\n-1 -2 0", ["a", "b"] }
        };

        return data;
    }
    public static TheoryData<string, string, string[]> ProvideRawCases()
    {
        var data = new TheoryData<string, string, string[]>
        {            
            // cnf -> .t0 & (!.t0 | !a | !b) & (.t0 | a) & (.t0 | b) -> reduced .t0 & (!.t0 | !a | !b)
            { "!a | !b", "p cnf 3 2\r\n1 0\r\n-1 -2 -3 0", [".t0", "a", "b"] },

            // 2o3
            { "a & b & !c | a & !b & c | !a & b & c", @"p cnf 11 15
1 0
-2 -10 0
2 -3 0
2 -7 0
-3 4 0
3 -5 0
-4 -7 0
4 -10 0
-5 -6 0
6 -8 0
6 -11 0
7 -8 0
10 -11 0
-1 9 11 0
5 8 -9 0", [".t7", "a", ".t0", "b", ".t1", "c", ".t2", ".t3", ".t4", ".t5", ".t6" ] }
        };

        return data;
    }
}
