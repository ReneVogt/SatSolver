using FluentAssertions;
using Revo.SatSolver.BooleanAlgebra;
using Revo.SatSolver.Parsing;

namespace SatSolverTests.BooleanAlgebra;

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
        var data = new TheoryData<string, string, string[]>();
        data.Add("0", "p cnf 0 1\r\n0", []);
        data.Add("1", "p cnf 0 0\r\n", []);
        data.Add("a", "p cnf 1 1\r\n1 0", ["a"]);
        data.Add("a & b", "p cnf 2 2\r\n1 0\r\n2 0", ["a", "b"]);
        data.Add("(a | b) & (!a | c) & (b | !c)", "p cnf 3 3\r\n-1 3 0\r\n1 2 0\r\n2 -3 0", ["a", "b", "c"]);
        return data;
    }
}
