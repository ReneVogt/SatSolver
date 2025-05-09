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
    public void Solve_NoLiterals_EmptySolution()
    {
        var solution = SatSolver.Solve(new(0, []));
        Assert.NotNull(solution);
        Assert.Empty(solution);
    }
    [Fact]
    public void Solve_EmptyClause_Null()
    {
        var solution = SatSolver.Solve(new(2, [new([1, 2]), new([1]), new([]), new([2])]));
        Assert.Null(solution);
    }
}
