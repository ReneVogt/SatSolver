using Revo.SatSolver;
using Revo.SatSolver.Parsing;

namespace SatSolverTests;

public sealed partial class SatSolverTests
{
    [Theory]
    [MemberData(nameof(ScanSatFiles))]
    public void Solve_SAT(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("SAT", fileName));
        var problem = DimacsParser.Parse(cnf).Single();
        Assert.True(SatSolver.IsSatisfiable(problem, out var solutions));        
        SolutionValidator.Validate(problem, solutions);
    }

    [Theory]
    [MemberData(nameof(ScanUnsatFiles))]
    public void Solve_UNSAT(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("UNSAT", fileName));
        var problem = DimacsParser.Parse(cnf).Single();
        Assert.False(SatSolver.IsSatisfiable(problem));
    }

    public static TheoryData<string> ScanSatFiles()
    {
        var data = new TheoryData<string>();
        data.AddRange(Directory.EnumerateFiles("SAT").Select(file => Path.GetFileName(file)).ToArray());
        return data;
    }
    public static TheoryData<string> ScanUnsatFiles()
    {
        var data = new TheoryData<string>();
        data.AddRange(Directory.EnumerateFiles("UNSAT").Select(file => Path.GetFileName(file)).ToArray());
        return data;
    }
}
