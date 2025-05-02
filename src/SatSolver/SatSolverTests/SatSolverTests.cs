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
}
