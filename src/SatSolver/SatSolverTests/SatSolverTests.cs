using Revo.SatSolver;

namespace SatSolverTests;

public sealed partial class SatSolverTests
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
}
