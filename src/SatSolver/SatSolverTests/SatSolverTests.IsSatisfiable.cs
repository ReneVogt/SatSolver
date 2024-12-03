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
        var problem = DimacsCnfParser.Parse(cnf).Single();
        Assert.True(SatSolver.IsSatisfiable(problem, out var solutions));        
        SolutionValidator.Validate(problem, solutions);
    }

    [Theory]
    [MemberData(nameof(ScanUnsatFiles))]
    public void Solve_UNSAT(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("UNSAT", fileName));
        var problem = DimacsCnfParser.Parse(cnf).Single();
        Assert.False(SatSolver.IsSatisfiable(problem));
    }

    public static IEnumerable<object[]> ScanSatFiles() => Directory.EnumerateFiles("SAT").Select(file => new object[] { Path.GetFileName(file) });
    public static IEnumerable<object[]> ScanUnsatFiles() => Directory.EnumerateFiles("UNSAT").Select(file => new object[] { Path.GetFileName(file) });
}
