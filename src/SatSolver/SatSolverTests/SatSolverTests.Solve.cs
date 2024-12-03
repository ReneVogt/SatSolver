using Revo.SatSolver;
using Revo.SatSolver.Parsing;

namespace SatSolverTests;

public sealed partial class SatSolverTests
{
    [Theory]
    [MemberData(nameof(ScanSolutionFiles))]
    public void Solve_Solutions(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("Solutions", fileName));
        var problems = DimacsCnfParser.Parse(cnf);
        var solutions = SatSolver.Solve(problems[0]).ToArray();
        AssertSameSolutions(problems[1].Clauses.Select(clause => clause.Literals).ToArray(), solutions);
    }

    void AssertSameSolutions(Literal[][] expectedSolutions, Literal[][] actualSolutions)
    {
        if (expectedSolutions.Length != actualSolutions.Length)
            Assert.Fail($"Different number of solutions: {expectedSolutions.Length} expected, actual {actualSolutions.Length}.");

        var expectedString = string.Join(Environment.NewLine, expectedSolutions.Select(solution => string.Join(" ", solution.Select(literal => literal.Sense ? literal.Id : -literal.Id).OrderBy(l => l))).OrderBy(s => s));
        var actualString = string.Join(Environment.NewLine, actualSolutions.Select(solution => string.Join(" ", solution.Select(literal => literal.Sense ? literal.Id : -literal.Id).OrderBy(l => l))).OrderBy(s => s));
        Assert.Equal(expectedString, actualString);
    }
    public static IEnumerable<object[]> ScanSolutionFiles() => Directory.EnumerateFiles("Solutions").Select(file => new object[] { Path.GetFileName(file) });

}
