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
        Assert.Throws<ArgumentNullException>(() => EnumerateSolutions(null!, null, default));
    }
    [Fact]
    public void EnumerateSolutions_NoLiterals_EmptySolution()
    {
        var solution = EnumerateSolutions(new Problem(0, [])).Single();
        Assert.NotNull(solution);
        Assert.Empty(solution);
    }
    [Fact]
    public void EnumerateSolutions_EmptyClause_NoSolution()
    {
        var solutions = EnumerateSolutions(new Problem(2, [new([1, 2]), new([1]), new([]), new([2])]));
        Assert.Empty(solutions);
    }
    [Fact]
    public void EnumerateSolutions_NoClauses_AllSolutions_CDCL()
    {
        var solutions = EnumerateSolutions(new Problem(3, []), SatSolverOptions.CDCL).ToArray();
        var clauses = solutions.Select(s => new Clause(s)).OrderBy(c => c).ToArray();
        Assert.Equal([
            [-1, -2, -3],
            [-1, -2, 3],
            [-1, 2, -3],
            [-1, 2, 3],
            [1, -2, -3],
            [1, -2, 3],
            [1, 2, -3],
            [1, 2, 3]], clauses.Select(c => c.Literals));
    }
    [Fact]
    public void EnumerateSolutions_NoClauses_AllSolutions_VSIDS()
    {
        var solutions = EnumerateSolutions(new Problem(3, []), SatSolverOptions.DPLL).ToArray();
        var clauses = solutions.Select(s => new Clause(s)).OrderBy(c => c).ToArray();
        Assert.Equal([
            [-1, -2, -3],
            [-1, -2, 3],
            [-1, 2, -3],
            [-1, 2, 3],
            [1, -2, -3],
            [1, -2, 3],
            [1, 2, -3],
            [1, 2, 3]], clauses.Select(c => c.Literals));
    }

    [Fact]
    public void EnumerateSolutions_InitialUnitPropagations_WithConflict() 
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        var unitsToPropagate = new UnitPropagationQueue();
        var sequence = new MockSequence();
        var propagator = new Mock<IPropagateVariables>(MockBehavior.Strict);
        var initializer = new Mock<IInitializeSatSolver>(MockBehavior.Strict);

        var options = SatSolverOptions.CDCL;
        var store = new ComponentStore(
            options,
            4,
            variables,
            unitsToPropagate,
            null!, null!,
            propagator.Object,
            null!, null!, null!, null!, [],
            null!, null!, null!,
            null!, default);

        // just for ignoring already set units
        unitsToPropagate.Enqueue((variables[4].PositiveLiteral, null));
        variables[4].Sense = true;

        var constraint0 = new Constraint([variables[3].NegativeLiteral]);
        unitsToPropagate.Enqueue((variables[3].NegativeLiteral, constraint0));
        var constraint1 = new Constraint([variables[1].PositiveLiteral]);
        unitsToPropagate.Enqueue((variables[1].PositiveLiteral, constraint1));

        var constraint2 = new Constraint([variables[3].PositiveLiteral, variables[0].PositiveLiteral]);
        var constraint3 = new Constraint([variables[1].NegativeLiteral, variables[2].NegativeLiteral]);

        var constraint4 = new Constraint([variables[3].NegativeLiteral, variables[2].NegativeLiteral]);

        initializer.InSequence(sequence).Setup(i => i.Initialize()).Returns(store);
        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variables[3], false, constraint0))
            .Callback(() => unitsToPropagate.Enqueue((variables[0].PositiveLiteral, constraint2)))
            .Returns((Constraint?)null);
        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variables[1], true, constraint1))
            .Callback(() => unitsToPropagate.Enqueue((variables[2].NegativeLiteral, constraint3)))
            .Returns((Constraint?)null);
        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variables[0], true, constraint2))
            .Returns((Constraint?)null);
        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variables[2], false, constraint3))
            .Returns(constraint4);

        Assert.Empty(SatSolver.EnumerateSolutions(initializer.Object));
        initializer.VerifyAll();
        initializer.VerifyNoOtherCalls();
        propagator.VerifyAll();
        propagator.VerifyNoOtherCalls();
    }
    [Fact]
    public void EnumerateSolutions_InitialUnitPropagations_NoConflict() 
    {
        var variable = new Variable(0);
        var variables = new[] { variable };
        var unitsToPropagate = new UnitPropagationQueue();
        var sequence = new MockSequence();
        var propagator = new Mock<IPropagateVariables>(MockBehavior.Strict);
        var initializer = new Mock<IInitializeSatSolver>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var heap = new Mock<ICandidateHeap>(MockBehavior.Strict);

        var options = SatSolverOptions.DPLL; // just to use that branch, too
        var store = new ComponentStore(
            options,
            4,
            variables,
            unitsToPropagate,
            heap.Object, trail.Object,
            propagator.Object,
            null!, null!, null!, null!, [],
            null!, null!, null!,
            null!, default);

        var constraint0 = new Constraint([variable.PositiveLiteral]);
        unitsToPropagate.Enqueue((variable.PositiveLiteral, constraint0));
        initializer.InSequence(sequence).Setup(i => i.Initialize()).Returns(store);
        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variable, true, constraint0))
            .Callback(() => variable.Sense = true)
            .Returns((Constraint?)null);
        trail.InSequence(sequence).Setup(t => t.Clear());
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Returns((Variable?)null);

        var solution = SatSolver.EnumerateSolutions(initializer.Object).First();
        Assert.Equal([1], solution);
        initializer.VerifyAll();
        initializer.VerifyNoOtherCalls();
        propagator.VerifyAll();
        propagator.VerifyNoOtherCalls();
        trail.VerifyAll();
        trail.VerifyNoOtherCalls();
        heap.VerifyAll();
        heap.VerifyNoOtherCalls();
    }

    [Fact]
    public void EnumerateSolutions_SimpleOr_NoConflicts()
    {
        var candidateHeap = new Mock<ICandidateHeap>(MockBehavior.Strict);
        var propagator = new Mock<IPropagateVariables>(MockBehavior.Strict);
        var restartManager = new Mock<IManageRestart>(MockBehavior.Strict);
        var conflictHandler = new Mock<IHandleConflicts>(MockBehavior.Strict);
        var reducer = new Mock<IReduceLearnedConstraints>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>();
        var activityManager = new Mock<IManageActivities>();
        var sequence = new MockSequence();

        var variables = Enumerable.Range(0, 2).Select(i => new Variable(i) { Polarity = true }).ToArray();
        var constraint = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral]);
        var unitsToPropagate = new UnitPropagationQueue();

        var options = SatSolverOptions.CDCL;
        var store = new ComponentStore(
            options,
            1,
            variables,
            unitsToPropagate,
            candidateHeap.Object,
            trail.Object,
            propagator.Object,
            conflictHandler.Object, null!, null!, null!, [],
            activityManager.Object, null!, null!,
            restartManager.Object, default);

        var initializer = new Mock<IInitializeSatSolver>();
        initializer.Setup(i => i.Initialize()).Returns(store);

        activityManager.Setup(am => am.ConstraintActivityIncrement).Returns(10);
        trail.Setup(t => t.Count).Returns(2);
        trail.Setup(t => t[0]).Returns(variables[0]);
        trail.Setup(t => t[1]).Returns(variables[1]);

        trail.InSequence(sequence).Setup(t => t.Clear());
        candidateHeap.InSequence(sequence).Setup(h => h.Dequeue()).Returns(variables[0]);
        trail.InSequence(sequence).Setup(t => t.Push(true));
        propagator.InSequence(sequence).Setup(p => p.PropagateVariable(variables[0], true, null)).Callback(() => variables[0].Sense = true).Returns((Constraint?)null);
        candidateHeap.InSequence(sequence).Setup(h => h.Dequeue()).Returns(variables[1]);
        trail.InSequence(sequence).Setup(t => t.Push(true));
        propagator.InSequence(sequence).Setup(p => p.PropagateVariable(variables[1], true, null)).Callback(() => variables[1].Sense = true).Returns((Constraint?)null);
        candidateHeap.InSequence(sequence).Setup(h => h.Dequeue()).Returns((Variable?)null);

        using var enumerator = SatSolver.EnumerateSolutions(initializer.Object).GetEnumerator();
        Assert.True(enumerator.MoveNext());
        var solution = enumerator.Current;
        Assert.Equal(["1", "2"], solution.Select(l => l.ToString()));

        conflictHandler.InSequence(sequence).Setup(handler => handler.HandleConflict(It.Is<Constraint>(constraint =>
            constraint.IsLearned && !constraint.IsTracked && constraint.Literals.SequenceEqual(variables.Select(v => v.NegativeLiteral)))));
        candidateHeap.InSequence(sequence).Setup(heap => heap.Dequeue()).Returns((Variable?)null);

        Assert.True(enumerator.MoveNext());

        candidateHeap.VerifyAll();
        propagator.VerifyAll();
        restartManager.VerifyAll();
        conflictHandler.VerifyAll();
        reducer.VerifyAll();
        candidateHeap.VerifyNoOtherCalls();
        propagator.VerifyNoOtherCalls();
        restartManager.VerifyNoOtherCalls();
        conflictHandler.VerifyNoOtherCalls();
        reducer.VerifyNoOtherCalls();
    }
    [Fact]
    public void EnumerateSolutions_ConflictOnDecisionLevelZero_NoSolution() 
    {
        var initializer = new Mock<IInitializeSatSolver>(MockBehavior.Strict);
        var heap = new Mock<ICandidateHeap>(MockBehavior.Strict);
        var propagator = new Mock<IPropagateVariables>(MockBehavior.Strict);
        var restartManager = new Mock<IManageRestart>(MockBehavior.Strict);
        var conflictHandler = new Mock<IHandleConflicts>(MockBehavior.Strict);
        var reducer = new Mock<IReduceLearnedConstraints>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);

        var sequence = new MockSequence();

        var variables = Enumerable.Range(0, 3).Select(i => new Variable(i) { Polarity = false }).ToArray();
        variables[2].Sense = true; // used to test that already assigned variables are ignored in the units queue

        var unitsToPropagate = new UnitPropagationQueue();

        var options = SatSolverOptions.CDCL;
        var store = new ComponentStore(
            options,
            1,
            variables,
            unitsToPropagate,
            heap.Object,
            trail.Object,
            propagator.Object,
            conflictHandler.Object, null!, null!, null!, [],
            null!, null!, null!,
            restartManager.Object, default);

#if DEBUG
        // setups for debug outputs
        trail.Setup(t => t.DecisionLevel).Returns(0);
#endif

        initializer.InSequence(sequence).Setup(i => i.Initialize()).Returns(store);

        trail.InSequence(sequence).Setup(t => t.Clear());
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Returns(variables[0]);
        trail.InSequence(sequence).Setup(t => t.Push(true));

        var reason = new Constraint([variables[2].NegativeLiteral]); // to avoid trail.Push() 
        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variables[0], false, null))
            .Callback(() =>
            {
                unitsToPropagate.Enqueue((variables[2].PositiveLiteral, null)); // to test ignorance
                unitsToPropagate.Enqueue((variables[1].PositiveLiteral, reason));
            })
            .Returns((Constraint?)null);

        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variables[1], true, reason))
            .Returns(reason);

        trail.InSequence(sequence).Setup(t => t.DecisionLevel).Returns(0);

        Assert.Empty(SatSolver.EnumerateSolutions(initializer.Object));

        trail.VerifyAll();
        trail.VerifyNoOtherCalls();
        heap.VerifyAll();
        propagator.VerifyAll();
        restartManager.VerifyAll();
        conflictHandler.VerifyAll();
        reducer.VerifyAll();
        heap.VerifyNoOtherCalls();
        propagator.VerifyNoOtherCalls();
        restartManager.VerifyNoOtherCalls();
        conflictHandler.VerifyNoOtherCalls();
        reducer.VerifyNoOtherCalls();
    }
    [Fact]
    public void EnumerateSolutions_Conflict_HandledReducedNoRestart()
    {
        var initializer = new Mock<IInitializeSatSolver>(MockBehavior.Strict);
        var heap = new Mock<ICandidateHeap>(MockBehavior.Strict);
        var propagator = new Mock<IPropagateVariables>(MockBehavior.Strict);
        var restartManager = new Mock<IManageRestart>(MockBehavior.Strict);
        var conflictHandler = new Mock<IHandleConflicts>(MockBehavior.Strict);
        var reducer = new Mock<IReduceLearnedConstraints>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var activityManager = new Mock<IManageActivities>(MockBehavior.Strict);
        var sequence = new MockSequence();
        var variable = new Variable(0);
        var variables = new[] { variable, new Variable(1) { Sense = true } };
        var constraint = new Constraint([variable.PositiveLiteral]);
        var unitsToPropagate = new UnitPropagationQueue();

        var options = SatSolverOptions.CDCL;
        var store = new ComponentStore(
            options,
            1,
            variables,
            unitsToPropagate,
            heap.Object,
            trail.Object,
            propagator.Object,
            conflictHandler.Object, null!,
            reducer.Object, null!, [],
            activityManager.Object, null!, null!,
            restartManager.Object, default);

        // setups for debug outputs
        var decisionLevel = 0;
#if DEBUG
        trail.Setup(t => t.DecisionLevel).Returns(() => decisionLevel);
#endif
        trail.Setup(t => t[0]).Returns(variable);
        trail.Setup(t => t[1]).Returns(variables[1]);
        trail.Setup(t => t.Count).Returns(2);

        activityManager.Setup(am => am.ConstraintActivityIncrement).Returns(10);

        initializer.InSequence(sequence).Setup(i => i.Initialize()).Returns(store);

        trail.InSequence(sequence).Setup(t => t.Clear());
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Returns(variable);
        trail.InSequence(sequence).Setup(t => t.Push(true)).Callback(() => decisionLevel++);

        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variable, false, null))
            .Returns(constraint);

        trail.InSequence(sequence).Setup(t => t.DecisionLevel).Returns(() => decisionLevel);

        conflictHandler.InSequence(sequence)
            .Setup(handler => handler.HandleConflict(constraint))
            .Callback(() => variable.Sense = true); // to exit via solution

        reducer.InSequence(sequence).Setup(r => r.ReduceLearnedConstraintsIfNecessary());
        restartManager.InSequence(sequence).Setup(rm => rm.RestartIfNecessary()).Returns(false);

        // now first leave with a solution
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Returns((Variable?)null);
        conflictHandler.InSequence(sequence)
            .Setup(handler => handler.HandleConflict(It.Is<Constraint>(c => c.Literals.SequenceEqual(new[] { variable.NegativeLiteral, variables[1].NegativeLiteral }))));

        // for the continuation
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Callback(() => variable.Sense = null).Returns(variable);
        trail.InSequence(sequence).Setup(t => t.Push(true));
        decisionLevel = 0;
        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variable, false, null))
            .Returns(constraint);
        trail.InSequence(sequence).Setup(t => t.DecisionLevel).Returns(0);

        var solution = Assert.Single(EnumerateSolutions(initializer.Object));
        Assert.Equal([1, 2], solution);

        activityManager.VerifyAll();
        trail.VerifyAll();
        trail.VerifyNoOtherCalls();
        heap.VerifyAll();
        propagator.VerifyAll();
        restartManager.VerifyAll();
        conflictHandler.VerifyAll();
        reducer.VerifyAll();
        heap.VerifyNoOtherCalls();
        propagator.VerifyNoOtherCalls();
        restartManager.VerifyNoOtherCalls();
        conflictHandler.VerifyNoOtherCalls();
        reducer.VerifyNoOtherCalls();
    }
    [Fact]
    public void EnumerateSolutions_Conflict_HandledReducedRestart()
    {
        var initializer = new Mock<IInitializeSatSolver>(MockBehavior.Strict);
        var heap = new Mock<ICandidateHeap>(MockBehavior.Strict);
        var propagator = new Mock<IPropagateVariables>(MockBehavior.Strict);
        var restartManager = new Mock<IManageRestart>(MockBehavior.Strict);
        var conflictHandler = new Mock<IHandleConflicts>(MockBehavior.Strict);
        var reducer = new Mock<IReduceLearnedConstraints>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var activityManager = new Mock<IManageActivities>(MockBehavior.Strict);

        var sequence = new MockSequence();
        var variable = new Variable(0);
        var variables = new[] { variable, new Variable(1) { Sense = false } };
        var constraint = new Constraint([variable.PositiveLiteral]);
        var unitsToPropagate = new UnitPropagationQueue();

        var options = SatSolverOptions.CDCL;
        var store = new ComponentStore(
            options,
            1,
            variables,
            unitsToPropagate,
            heap.Object,
            trail.Object,
            propagator.Object,
            conflictHandler.Object, null!,
            reducer.Object, null!, [],
            activityManager.Object, null!, null!,
            restartManager.Object, default);

        // setups for debug outputs
        var decisionLevel = 0;
#if DEBUG
        trail.Setup(t => t.DecisionLevel).Returns(() => decisionLevel);
#endif
        trail.Setup(t => t[0]).Returns(variable);
        trail.Setup(t => t[1]).Returns(variables[1]);
        trail.Setup(t => t.Count).Returns(2);
        activityManager.Setup(am => am.ConstraintActivityIncrement).Returns(10);
        initializer.InSequence(sequence).Setup(i => i.Initialize()).Returns(store);

        trail.InSequence(sequence).Setup(t => t.Clear());
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Returns(variable);
        trail.InSequence(sequence).Setup(t => t.Push(true)).Callback(() => decisionLevel++);

        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variable, false, null))
            .Returns(constraint);

        trail.InSequence(sequence).Setup(t => t.DecisionLevel).Returns(() => decisionLevel);

        conflictHandler.InSequence(sequence)
            .Setup(handler => handler.HandleConflict(constraint))
            .Callback(() => variable.Sense = false); // to exit via solution

        reducer.InSequence(sequence).Setup(r => r.ReduceLearnedConstraintsIfNecessary());
        restartManager.InSequence(sequence).Setup(rm => rm.RestartIfNecessary()).Returns(true);

        // now first leave with a solution
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Returns((Variable?)null);
        conflictHandler.InSequence(sequence)
            .Setup(handler => handler.HandleConflict(It.Is<Constraint>(c => c.Literals.SequenceEqual(new[] { variable.PositiveLiteral, variables[1].PositiveLiteral }))));

        // for the continuation
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Callback(() => variable.Sense = null).Returns(variable);
        trail.InSequence(sequence).Setup(t => t.Push(true));
        decisionLevel = 0;
        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variable, false, null))
            .Returns(constraint);
        trail.InSequence(sequence).Setup(t => t.DecisionLevel).Returns(0);

        var solution = Assert.Single(SatSolver.EnumerateSolutions(initializer.Object));
        Assert.Equal([-1, -2], solution);

        activityManager.VerifyAll();
        trail.VerifyAll();
        trail.VerifyNoOtherCalls();
        heap.VerifyAll();
        propagator.VerifyAll();
        restartManager.VerifyAll();
        conflictHandler.VerifyAll();
        reducer.VerifyAll();
        heap.VerifyNoOtherCalls();
        propagator.VerifyNoOtherCalls();
        restartManager.VerifyNoOtherCalls();
        conflictHandler.VerifyNoOtherCalls();
        reducer.VerifyNoOtherCalls();
    }
}