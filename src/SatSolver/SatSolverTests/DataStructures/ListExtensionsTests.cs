using Revo.SatSolver.DataStructures;

namespace SatSolverTests.DataStructures;

public sealed class ListExtensionsTests
{
    [Fact]
    public void SwapRemove_FirstOfSingle()
    {
        var sut = new List<Constraint> { new([], null!, null!) };
        sut.SwapRemove(0);
        Assert.Empty(sut);
    }
    [Fact]
    public void SwapRemove_FirstOfMultiple()
    {
        var c0 = new Constraint([], null!, null!);
        var c1 = new Constraint([], null!, null!);
        var c2 = new Constraint([], null!, null!);

        var sut = new List<Constraint> { c0, c1, c2 };
        sut.SwapRemove(0);
        Assert.Equal([c2, c1], sut);
    }
    [Fact]
    public void SwapRemove_Middle()
    {
        var c0 = new Constraint([], null!, null!);
        var c1 = new Constraint([], null!, null!);
        var c2 = new Constraint([], null!, null!);
        var c3 = new Constraint([], null!, null!);

        var sut = new List<Constraint> { c0, c1, c2, c3 };
        sut.SwapRemove(1);
        Assert.Equal([c0, c3, c2], sut);
    }
    [Fact]
    public void SwapRemove_Last()
    {
        var c0 = new Constraint([], null!, null!);
        var c1 = new Constraint([], null!, null!);
        var c2 = new Constraint([], null!, null!);

        var sut = new List<Constraint> { c0, c1, c2 };
        sut.SwapRemove(2);
        Assert.Equal([c0, c1], sut);
    }
}
