using Revo.SatSolver;
using Revo.SatSolver.DataStructures;
using TestHelpers;
using static Revo.SatSolver.SatSolverFactory;

namespace SatSolverTests.ComponentTests.Solving;

[Trait("TestLevel", "Component")]
public sealed class RestartPropagationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void FindSolution_RestartAfterLearningUnit_PropagatesRootChainBeforeNextDecision(bool luby, bool reduceOnRestart)
    {
        // Deciding -x conflicts through y, learns (x), and backjumps to root.
        // The immediately following restart must preserve x and hence imply z.
        var problem = new Problem(3, [new([1, 2]), new([1, -2]), new([-1, 3])]);
        var store = new ComponentStore(Options(luby, reduceOnRestart), problem);
        var solver = Create(store);
        var x = store.Variables[0];
        var y = store.Variables[1];
        var z = store.Variables[2];
        store.UnitPropagationQueue.Enqueue((x.NegativeLiteral, null));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var solution = solver.FindSolution(timeout.Token);

        Assert.NotNull(solution);
        SolutionValidator.Validate(problem, solution);
        Assert.True(x.Sense);
        Assert.Equal(0, x.DecisionLevel);
        Assert.NotNull(x.Reason);
        Assert.True(x.Reason.IsLearned);
        Assert.Same(x.PositiveLiteral, Assert.Single(x.Reason.Literals));
        // Losing the first implication can still eventually solve with Luby,
        // but only after deciding -x again and relearning the same unit.
        Assert.Same(x.Reason, Assert.Single(x.PositiveLiteral.Watchers, c => c.IsLearned));
        Assert.True(z.Sense);
        Assert.Equal(0, z.DecisionLevel);
        Assert.NotNull(z.Reason);
        Assert.Equal(new[] { x.NegativeLiteral, z.PositiveLiteral }, z.Reason.Literals);
        Assert.Same(x, store.VariableTrail[0]);
        Assert.Same(z, store.VariableTrail[1]);
        Assert.Same(y, store.VariableTrail[2]);
        Assert.Equal(1, y.DecisionLevel);
        Assert.Null(y.Reason);
        Assert.Empty(store.UnitPropagationQueue);
        // The conflict's restart has already been consumed by the solver.
        Assert.False(store.RestartManager.RestartIfNecessary());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Restart_LearnedClauseUnitUnderRetainedRootAssignments_PreservesImplication(int clauseLength)
    {
        var problem = new Problem(5,
            [new([1]), new([2]), new([3, 4]), new([3, -4]), new([-3, 5])]);
        var store = new ComponentStore(Options(false, true), problem);
        var solver = Create(store);
        while (store.UnitPropagationQueue.TryDequeue(out var entry))
            Assert.Null(store.VariablePropagator.PropagateVariable(
                entry.UnitLiteral.Variable, entry.UnitLiteral.Orientation, entry.Reason));
        var r = store.Variables[0];
        var s = store.Variables[1];
        var x = store.Variables[2];
        var z = store.Variables[4];
        var rootReason = r.Reason;
        store.VariableTrail.Push();
        Assert.Null(store.VariablePropagator.PropagateVariable(x, false, null));

        // Construct a valid learned clause directly: minimization would remove
        // its root literals. Cover both the binary and general watcher paths.
        ConstraintLiteral[] literals = clauseLength == 2
            ? [x.PositiveLiteral, r.NegativeLiteral]
            : [x.PositiveLiteral, r.NegativeLiteral, s.NegativeLiteral];
        var learned = store.ConstraintFactory.CreateLearnedConstraint(literals,
            store.VariableTrail.DecisionLevel, 1, 8, 2, out var jumpBackLevel);
        Assert.Equal(0, jumpBackLevel);
        store.UnitPropagationQueue.Clear();
        store.UnitPropagationQueue.Enqueue((learned.Watched1, learned));
        store.VariableTrail.JumpBack(jumpBackLevel);
        store.RestartManager.AddConflict();

        Assert.True(store.RestartManager.RestartIfNecessary());

        var pending = Assert.Single(store.UnitPropagationQueue);
        Assert.Same(x.PositiveLiteral, pending.UnitLiteral);
        Assert.Same(learned, pending.Reason);
        Assert.True(r.Sense);
        Assert.True(s.Sense);
        Assert.Equal(2, store.VariableTrail.Count);
        Assert.Same(rootReason, r.Reason);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var solution = solver.FindSolution(timeout.Token);
        Assert.NotNull(solution);
        SolutionValidator.Validate(problem, solution);
        Assert.True(x.Sense);
        Assert.Equal(0, x.DecisionLevel);
        Assert.Same(learned, x.Reason);
        Assert.True(z.Sense);
        Assert.Equal(0, z.DecisionLevel);
        Assert.NotNull(z.Reason);
        Assert.Same(x, store.VariableTrail[2]);
        Assert.Same(z, store.VariableTrail[3]);
    }

    static SatSolverOptions Options(bool luby, bool reduceOnRestart) => new()
    {
        Restart = new()
        {
            Interval = 1,
            Luby = luby,
            ByLiteralBlockDistance = false,
            ByPropagationRate = false
        },
        ConstraintDeletion = new() { ReduceOnRestart = reduceOnRestart }
    };
}
