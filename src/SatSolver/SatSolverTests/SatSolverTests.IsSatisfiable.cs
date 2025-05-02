using Revo.SatSolver;
using Revo.SatSolver.Parsing;
using Xunit.Abstractions;

namespace SatSolverTests;

public sealed partial class SatSolverTests(ITestOutputHelper? output)
{
    [Theory]
    [MemberData(nameof(ScanSatFiles))]
    public void Solve_SAT(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("SAT", fileName));
        var problem = DimacsParser.Parse(cnf).Single();
        output?.WriteLine($"{fileName} Literals: {problem.NumberOfLiterals} Clauses: {problem.Clauses.Length}");
        var solution = SatSolver.Solve(problem);
        Assert.NotNull(solution);        
        SolutionValidator.Validate(problem, solution);
    }

    [Theory]
    [MemberData(nameof(ScanUnsatFiles))]
    public void Solve_UNSAT(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("UNSAT", fileName));
        var problem = DimacsParser.Parse(cnf).Single();
        output?.WriteLine($"{fileName} Literals: {problem.NumberOfLiterals} Clauses: {problem.Clauses.Length}");
        Assert.Null(SatSolver.Solve(problem));
    }

    public static TheoryData<string> ScanSatFiles()
    {
        var data = new TheoryData<string>();
        data.AddRange([.. Directory.EnumerateFiles("SAT").Select(file => Path.GetFileName(file))]);
        return data;
    }
    public static TheoryData<string> ScanUnsatFiles()
    {
        var data = new TheoryData<string>();
        data.AddRange([.. Directory.EnumerateFiles("UNSAT").Select(file => Path.GetFileName(file))]);
        return data;
    }
}
