using Revo.SatSolver;

namespace SatSolverTests;

public sealed class SatSolverTests
{
    [Theory]
    [MemberData(nameof(ScanSatFiles))]
    public void Solve_SAT(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("SAT", fileName));
        var problem = DimacsCnfParser.Parse(cnf).Single();
        Assert.NotEmpty(SatSolver.Solve(problem));
    }

    [Theory]
    [MemberData(nameof(ScanUnsatFiles))]
    public void Solve_UNSAT(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("UNSAT", fileName));
        var problem = DimacsCnfParser.Parse(cnf).Single();
        Assert.Empty(SatSolver.Solve(problem));
    }

    public static IEnumerable<object[]> ScanSatFiles() => Directory.EnumerateFiles("SAT").Select(file => new object[] { Path.GetFileName(file) });
    public static IEnumerable<object[]> ScanUnsatFiles() => Directory.EnumerateFiles("UNSAT").Select(file => new object[] { Path.GetFileName(file) });
}
