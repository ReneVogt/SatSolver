using Revo.SatSolver;
using Revo.SatSolver.Parsing;
using System.Collections.Immutable;

namespace SatSolverTests;

public sealed partial class SatSolverTests
{
    [Theory]
    [MemberData(nameof(ScanSolutionFiles))]
    public void Solve_Solutions(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("Solutions", fileName));
        var problem = DimacsParser.Parse(cnf).Single();
        var solution = SatSolver.Solve(problem);
        Assert.NotNull(solution);
        SolutionValidator.Validate(problem, solution);
    }

    public static TheoryData<string> ScanSolutionFiles()
    {
        var data = new TheoryData<string>();
        data.AddRange([.. Directory.EnumerateFiles("Solutions").Select(file => Path.GetFileName(file))]);
        return data;
    }

}
