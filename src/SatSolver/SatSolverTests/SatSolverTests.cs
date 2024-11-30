using Revo.SatSolver;

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

    /// <summary>
    /// XOR
    ///   a & -b            |       -a & b
    ///   a&-b  | -a        &    a&-b | b
    ///   a|-a  &  -b|-a    &    a|b  &  -b|b
    /// </summary>
    [Fact]
    public void Solve_XOR_Solved()
    {
        var problem = DimacsCnfParser.Parse(@"p cnf 2 4
1 -1 0
-2 -1 0
1 2 0
-2 2 0
%").Single();

        const string expected = @"-1 2
1 -2";
        var solution = string.Join(Environment.NewLine, SatSolver.Solve(problem).Select(s => string.Join(" ", s.OrderBy(l => l.Id).ThenBy(l => l.Sense).Select(l => l.Sense ? l.Id : -l.Id))).OrderBy(s => s));
        Assert.Equal(expected, solution);
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
