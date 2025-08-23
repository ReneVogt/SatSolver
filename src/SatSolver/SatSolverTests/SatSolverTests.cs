using Moq;
using Revo.SatSolver;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;
using Xunit.Abstractions;
using static Revo.SatSolver.SatSolver;

namespace SatSolverTests;

public sealed partial class SatSolverTests(ITestOutputHelper _output)
{
    [Fact]
    public void EnumerateSolutions_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EnumerateSolutions(null!));
    }
    [Fact]
    public void EnumerateSolutions_NoLiterals_EmptySolution()
    {
        var solution = EnumerateSolutions(new(0, [])).Single();
        Assert.NotNull(solution);
        Assert.Empty(solution);
    }
    [Fact]
    public void EnumerateSolutions_EmptyClause_NoSolution()
    {
        var solutions = EnumerateSolutions(new(2, [new([1, 2]), new([1]), new([]), new([2])]));
        Assert.Empty(solutions);
    }

    [Fact]
    public void EnumerateSolutions_SimpleOr_NoConflicts()
    {
        var variables = Enumerable.Range(0, 2).Select(i => new Variable(i)).ToArray();
        var constraint = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral]);
        
        var candidateHeap = new Mock<ICandidateHeap>();
        candidateHeap.SetupSequence(heap => heap.Dequeue())
            .Returns(variables[0])
            .Returns(variables[1])
            .Returns((Variable?)null);

        var propagator = new Mock<IPropagateVariables>();

        var propagationRateTracker = new Mock<ITrackPropagationRate>();

        var restartManager = new Mock<IManageRestart>();

        var options = SatSolverOptions.CDCL;
        var store = new ComponentStore(
            options,
            1,
            variables,
            [],
            candidateHeap.Object,
            null!,
            propagator.Object,
            null!, null!, null!, null!, [],
            null!, null!,
            propagationRateTracker.Object,
            restartManager.Object, default);        
    }
}