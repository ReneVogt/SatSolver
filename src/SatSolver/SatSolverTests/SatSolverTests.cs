using Revo.SatSolver;
using Revo.SatSolver.Parsing;

namespace SatSolverTests;

public sealed class SatSolverTests
{
    [Fact]
    public void Solve_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SatSolver.Solve(null!));
    }
    [Fact]
    public void Solve_NoLiterals_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SatSolver.Solve(new(0, [new([])])));
    }
    [Fact]
    public void Solve_InvalidLiterals_ArgumentException()
    {
        var problem = new Problem(5, [new([new Literal(1, true), new Literal(2, false), new Literal(17, true)])]);
        Assert.Throws<ArgumentException>(() => SatSolver.Solve(problem));
    }

    [
        Theory,
        InlineData("c Basic1\np cnf 3 1\n1 2 3 0\n", "1|2|3"),
        InlineData("c XOR\np cnf 2 4\n1 -1 0\n-2 -1 0\n1 2 0\n-2 2 0", "-1 2|1 -2"),
        InlineData("c 2o3\np cnf 3 8\n1 2 3 0\n-1 -2 -3 0\n-1 2 3 0\n1 -2 3 0\n1 2 -3 0\n1 2 0\n1 3 0\n2 3 0 \n", "-1 2 3|1 -2 3|1 2 -3")
    ]
    public void Solve_Cnf_Solutions(string cnf, string expectedSolutions)
    {
        var problem = DimacsCnfParser.Parse(cnf).Single();
        var solutions = string.Join("|", SatSolver.Solve(problem).Select(s => string.Join(" ", s.OrderBy(l => l.Id).ThenBy(l => l.Sense).Select(l => l.Sense ? l.Id : -l.Id))).OrderBy(s => s));
        Assert.Equal(expectedSolutions, solutions);
    }


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
