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
    [MemberData(nameof(ProvideCases))]
    public void ToProblem_Correct(string input, string expected, string[] literals)
    {
        BooleanAlgebraParser.Parse(input).ToProblem(out var mapping).ToString().Should().Be(expected);
        mapping.Count.Should().Be(literals.Length);
        for (var i = 0; i< literals.Length; i++)
            mapping[literals[i]].Should().Be(i+1);
    }

    public static TheoryData<string, string, string[]> ProvideCases()
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

            // 2o3
            { "a & b & !c | a & !b & c | !a & b & c", @"p cnf 3 4
1 2 0
1 3 0
2 3 0
-1 -2 -3 0", ["a", "b", "c"] }
        };

        return data;
    }
}
