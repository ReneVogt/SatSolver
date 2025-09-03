using Moq;
using Revo.SatSolver;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;
using Xunit.Abstractions;
using static Revo.SatSolver.SatSolverFactory;

namespace SatSolverTests;

public sealed partial class SatSolverTests(ITestOutputHelper _output)
{
    static readonly ConstraintFactory _constraintFactory = new([]);

    [Fact]
    public void Constructor_CorrectInitialization()
    {
        var preProcessor = new Mock<IPreProcessor>(MockBehavior.Strict);
        var heap = new Mock<ICandidateHeap>(MockBehavior.Strict);
        var store = new TestComponentStore(new(), 0, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            _ => null!
        });

        var seq = new MockSequence();
        preProcessor.InSequence(seq).Setup(p => p.BuildConstraints()).Returns(0);
        heap.InSequence(seq).Setup(h => h.Heapify());

        _ = Create(store);
        preProcessor.VerifyAll();
        heap.VerifyAll();
    }

    [Fact]
    public void FindSolution_Canceled_OperationCanceledException()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        using var cts = new CancellationTokenSource();
        var store = new TestComponentStore(new(), 0, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            _ => null!
        }, cts.Token);
        var sut = Create(store);
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => sut.FindSolution());
    }
    [Fact]
    public void FindSolution_InitialUnitPropagations_WithConflict()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var propagator = new Mock<IPropagateVariables>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        trail.Setup(t => t.DecisionLevel).Returns(0);
        var store = new TestComponentStore(SatSolverOptions.CDCL, 5, name => name switch
        {
            nameof(ComponentStoreBase.VariablePropagator) => propagator.Object,
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var variables = store.Variables;
        var unitsToPropagate = store.UnitPropagationQueue;
        var sequence = new MockSequence();
        var sut = Create(store);

        // just for ignoring already set units
        unitsToPropagate.Enqueue((variables[4].PositiveLiteral, null));
        variables[4].Sense = true;

        var constraint0 = _constraintFactory.CreateInitialConstraint([variables[3].NegativeLiteral]);
        unitsToPropagate.Enqueue((variables[3].NegativeLiteral, constraint0));
        var constraint1 = _constraintFactory.CreateInitialConstraint([variables[1].PositiveLiteral]);
        unitsToPropagate.Enqueue((variables[1].PositiveLiteral, constraint1));

        var constraint2 = _constraintFactory.CreateInitialConstraint([variables[3].PositiveLiteral, variables[0].PositiveLiteral]);
        var constraint3 = _constraintFactory.CreateInitialConstraint([variables[1].NegativeLiteral, variables[2].NegativeLiteral]);

        var constraint4 = _constraintFactory.CreateInitialConstraint([variables[3].NegativeLiteral, variables[2].NegativeLiteral]);

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

        Assert.Null(sut.FindSolution());
        propagator.VerifyAll();
    }
    [Fact]
    public void FindSolution_InitialUnitPropagations_NoConflict()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var heap = new Mock<ICandidateHeap>(MockBehavior.Strict);
        var propagator = new Mock<IPropagateVariables>(MockBehavior.Strict);
#if DEBUG
    trail.Setup(t => t.DecisionLevel).Returns(0);
#endif
        var store = new TestComponentStore(SatSolverOptions.CDCL, 1, name => name switch
        {
            nameof(ComponentStoreBase.VariablePropagator) => propagator.Object,
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var variables = store.Variables;
        var unitsToPropagate = store.UnitPropagationQueue;
        var sequence = new MockSequence();

        var variable = variables[0];

        var constraint0 = _constraintFactory.CreateInitialConstraint([variable.PositiveLiteral]);
        unitsToPropagate.Enqueue((variable.PositiveLiteral, constraint0));

        heap.InSequence(sequence).Setup(h => h.Heapify());
        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variable, true, constraint0))
            .Callback(() => variable.Sense = true)
            .Returns((Constraint?)null);
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Returns((Variable?)null);

        var sut = Create(store);
        var solution = sut.FindSolution();
        Assert.Equal([1], solution!);
        propagator.VerifyAll();
        trail.VerifyAll();
        heap.VerifyAll();
    }
    [Fact]
    public void FindSolution_SimpleOr_NoConflicts()
    {
        var candidateHeap = new Mock<ICandidateHeap>(MockBehavior.Strict);
        var propagator = new Mock<IPropagateVariables>(MockBehavior.Strict);
        var restartManager = new Mock<IManageRestart>(MockBehavior.Strict);
        var conflictHandler = new Mock<IHandleConflicts>(MockBehavior.Strict);
        var reducer = new Mock<IReduceLearnedConstraints>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>();
        var activityManager = new Mock<IManageActivities>();
        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var sequence = new MockSequence();
        var preProcessor = new Mock<IPreProcessor>();
        var store = new TestComponentStore(SatSolverOptions.CDCL, 2, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => candidateHeap.Object,
            nameof(ComponentStoreBase.VariablePropagator) => propagator.Object,
            nameof(ComponentStoreBase.RestartManager) => restartManager.Object,
            nameof(ComponentStoreBase.ConflictHandler) => conflictHandler.Object,
            nameof(ComponentStoreBase.LearnedConstraintsReducer) => reducer.Object,
            nameof(ComponentStoreBase.ActivityManager) => activityManager.Object,
            nameof(ComponentStoreBase.ConstraintFactory) => constraintFactory.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var variables = store.Variables;
        var unitsToPropagate = store.UnitPropagationQueue;
        variables[0].Activity = 1;
        variables[0].Polarity = true;
        variables[1].Polarity = true;
        var constraint = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral]);

        candidateHeap.InSequence(sequence).Setup(h => h.Heapify());
        candidateHeap.InSequence(sequence).Setup(h => h.Dequeue()).Returns(variables[0]);
        trail.InSequence(sequence).Setup(t => t.Push(true));
        propagator.InSequence(sequence).Setup(p => p.PropagateVariable(variables[0], true, null)).Callback(() => variables[0].Sense = true).Returns((Constraint?)null);
        candidateHeap.InSequence(sequence).Setup(h => h.Dequeue()).Returns(variables[1]);
        trail.InSequence(sequence).Setup(t => t.Push(true));
        propagator.InSequence(sequence).Setup(p => p.PropagateVariable(variables[1], true, null)).Callback(() => variables[1].Sense = true).Returns((Constraint?)null);
        candidateHeap.InSequence(sequence).Setup(h => h.Dequeue()).Returns((Variable?)null);

        var sut = Create(store);
        var solution = sut.FindSolution();
        Assert.Equal(["1", "2"], solution!.Select(l => l.ToString()));

        candidateHeap.VerifyAll();
        propagator.VerifyAll();
        restartManager.VerifyAll();
        conflictHandler.VerifyAll();
        reducer.VerifyAll();
    }
    [Fact]
    public void EnumerateSolutions_ConflictOnDecisionLevelZero_NoSolution()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>(MockBehavior.Strict);
        var propagator = new Mock<IPropagateVariables>(MockBehavior.Strict);
        var restartManager = new Mock<IManageRestart>(MockBehavior.Strict);
        var conflictHandler = new Mock<IHandleConflicts>(MockBehavior.Strict);
        var reducer = new Mock<IReduceLearnedConstraints>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);

        var store = new TestComponentStore(SatSolverOptions.CDCL, 3, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.VariablePropagator) => propagator.Object,
            nameof(ComponentStoreBase.RestartManager) => restartManager.Object,
            nameof(ComponentStoreBase.ConflictHandler) => conflictHandler.Object,
            nameof(ComponentStoreBase.LearnedConstraintsReducer) => reducer.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var variables = store.Variables;
        var unitsToPropagate = store.UnitPropagationQueue;
        var sequence = new MockSequence();

        variables[2].Sense = true; // used to test that already assigned variables are ignored in the units queue

#if DEBUG
        // setups for debug outputs
        trail.Setup(t => t.DecisionLevel).Returns(0);
#endif
        heap.InSequence(sequence).Setup(h => h.Heapify());
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Returns(variables[0]);
        trail.InSequence(sequence).Setup(t => t.Push(true));

        var reason = _constraintFactory.CreateInitialConstraint([variables[2].NegativeLiteral]); // to avoid trail.Push() 
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

        var sut = Create(store);
        Assert.Null(sut.FindSolution());

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
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>(MockBehavior.Strict);
        var propagator = new Mock<IPropagateVariables>(MockBehavior.Strict);
        var restartManager = new Mock<IManageRestart>(MockBehavior.Strict);
        var conflictHandler = new Mock<IHandleConflicts>(MockBehavior.Strict);
        var reducer = new Mock<IReduceLearnedConstraints>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var activityManager = new Mock<IManageActivities>(MockBehavior.Strict);
        var sequence = new MockSequence();
        var store = new TestComponentStore(SatSolverOptions.CDCL, 2, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.VariablePropagator) => propagator.Object,
            nameof(ComponentStoreBase.RestartManager) => restartManager.Object,
            nameof(ComponentStoreBase.ConflictHandler) => conflictHandler.Object,
            nameof(ComponentStoreBase.LearnedConstraintsReducer) => reducer.Object,
            nameof(ComponentStoreBase.ActivityManager) => activityManager.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var variables = store.Variables;
        var variable = variables[0];
        variables[1].Sense = true;
        var unitsToPropagate = store.UnitPropagationQueue;
        var constraint = _constraintFactory.CreateInitialConstraint([variable.PositiveLiteral]);

        // setups for debug outputs
        var decisionLevel = 0;
#if DEBUG
        trail.Setup(t => t.DecisionLevel).Returns(() => decisionLevel);
#endif

        preProcessor.Setup(p => p.BuildConstraints()).Returns(17);
        heap.InSequence(sequence).Setup(h => h.Heapify());
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Returns(variable);
        trail.InSequence(sequence).Setup(t => t.Push(true)).Callback(() => decisionLevel++);

        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variable, false, null))
            .Returns(constraint);

        trail.InSequence(sequence).Setup(t => t.DecisionLevel).Returns(() => decisionLevel);

        conflictHandler.InSequence(sequence)
            .Setup(handler => handler.HandleConflict(constraint))
            .Callback(() => variable.Sense = true); // to exit via solution

        reducer.InSequence(sequence).Setup(r => r.ReduceLearnedConstraintsIfNecessary(17));
        restartManager.InSequence(sequence).Setup(rm => rm.RestartIfNecessary()).Returns(false);

        // now first leave with a solution
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Returns((Variable?)null);
        var conflictingConstraint = new Constraint([], null!, null!);
        const int activity = 12;
        activityManager.InSequence(sequence).Setup(am => am.ConstraintActivityIncrement).Returns(activity);
        conflictHandler.InSequence(sequence)
            .Setup(handler => handler.HandleConflict(conflictingConstraint));

        var sut = Create(store);
        var solution = sut.FindSolution();
        Assert.Equal([1, 2], solution!);

        activityManager.VerifyAll();
        trail.VerifyAll();
        heap.VerifyAll();
        propagator.VerifyAll();
        restartManager.VerifyAll();
        conflictHandler.VerifyAll();
        reducer.VerifyAll();
    }
    [Fact]
    public void EnumerateSolutions_Conflict_HandledReducedRestart()
    {
        var preProcessor = new Mock<IPreProcessor>(MockBehavior.Strict);
        var heap = new Mock<ICandidateHeap>(MockBehavior.Strict);
        var propagator = new Mock<IPropagateVariables>(MockBehavior.Strict);
        var restartManager = new Mock<IManageRestart>(MockBehavior.Strict);
        var conflictHandler = new Mock<IHandleConflicts>(MockBehavior.Strict);
        var reducer = new Mock<IReduceLearnedConstraints>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var activityManager = new Mock<IManageActivities>(MockBehavior.Strict);
        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);

        var sequence = new MockSequence();

        var store = new TestComponentStore(SatSolverOptions.CDCL, 2, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.VariablePropagator) => propagator.Object,
            nameof(ComponentStoreBase.RestartManager) => restartManager.Object,
            nameof(ComponentStoreBase.ConflictHandler) => conflictHandler.Object,
            nameof(ComponentStoreBase.LearnedConstraintsReducer) => reducer.Object,
            nameof(ComponentStoreBase.ActivityManager) => activityManager.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            nameof(ComponentStoreBase.ConstraintFactory) => constraintFactory.Object,
            _ => null!
        });

        var variables = store.Variables; 
        var variable = variables[0];
        variables[1].Sense = false;
        var constraint = _constraintFactory.CreateInitialConstraint([variable.PositiveLiteral]);
        var unitsToPropagate = store.UnitPropagationQueue;

        // setups for debug outputs
        var decisionLevel = 0;
#if DEBUG
        trail.Setup(t => t.DecisionLevel).Returns(() => decisionLevel);
#endif
        preProcessor.InSequence(sequence).Setup(p => p.BuildConstraints()).Returns(2);

        heap.InSequence(sequence).Setup(h => h.Heapify());
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Returns(variable);
        trail.InSequence(sequence).Setup(t => t.Push(true)).Callback(() => decisionLevel++);

        propagator.InSequence(sequence)
            .Setup(p => p.PropagateVariable(variable, false, null))
            .Returns(constraint);

        trail.InSequence(sequence).Setup(t => t.DecisionLevel).Returns(() => decisionLevel);

        conflictHandler.InSequence(sequence)
            .Setup(handler => handler.HandleConflict(constraint))
            .Callback(() => variable.Sense = false); // to exit via solution

        reducer.InSequence(sequence).Setup(r => r.ReduceLearnedConstraintsIfNecessary(2));
        restartManager.InSequence(sequence).Setup(rm => rm.RestartIfNecessary()).Returns(true);

        // now first leave with a solution
        heap.InSequence(sequence).Setup(h => h.Dequeue()).Returns((Variable?)null);
        activityManager.InSequence(sequence).Setup(am => am.ConstraintActivityIncrement).Returns(10);
        var conflictSolution = new Constraint([], null!, null!);

        var sut = Create(store);
        var solution = sut.FindSolution();
        Assert.Equal([-1, -2], solution!);

        activityManager.VerifyAll();
        trail.VerifyAll();
        heap.VerifyAll();
        propagator.VerifyAll();
        restartManager.VerifyAll();
        conflictHandler.VerifyAll();
        reducer.VerifyAll();
    }

    [Fact]
    public void Reset_Canceled_OperationCanceledException()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        using var cts = new CancellationTokenSource();
        var store = new TestComponentStore(new(), 0, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            _ => null!
        }, cts.Token);

        var sut = Create(store);
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => sut.Reset(false));
        Assert.Throws<OperationCanceledException>(() => sut.Reset(true));
    }
    [Fact]
    public void Reset_NoRemove_TrailResetUnitsInitialized()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);

        var store = new TestComponentStore(new(), 2, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.ConstraintFactory) => constraintFactory.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var unitsToPropagate = store.UnitPropagationQueue;
        var constraint = new Constraint([store.Variables[0].PositiveLiteral, store.Variables[1].NegativeLiteral],
            store.Variables[0].PositiveLiteral,
            store.Variables[1].NegativeLiteral);
        constraint.Watched1.Watchers.Add(constraint);
        constraint.Watched2.Watchers.Add(constraint);
        constraint.IsAdditional = constraint.IsLearned = true;

        var unitConstraint = new Constraint([store.Variables[0].PositiveLiteral],
            store.Variables[0].PositiveLiteral, store.Variables[0].PositiveLiteral);
        unitConstraint.Watched1.Watchers.Add(unitConstraint);

        unitsToPropagate.Enqueue((store.Variables[1].PositiveLiteral, constraint));
        var sut = Create(store);

        trail.Setup(t => t.Reset());

        sut.Reset();

        Assert.Equal([(store.Variables[0].PositiveLiteral, unitConstraint)], unitsToPropagate);
        trail.VerifyAll();
    }
    [Fact]
    public void Reset_RemovedAdditionalAndLearnd_TrailResetUnitsInitialized()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);

        var store = new TestComponentStore(new(), 2, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.ConstraintFactory) => constraintFactory.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var unitsToPropagate = store.UnitPropagationQueue;
        var constraint = new Constraint([store.Variables[0].PositiveLiteral, store.Variables[1].NegativeLiteral],
            store.Variables[0].PositiveLiteral,
            store.Variables[1].NegativeLiteral);
        constraint.Watched1.Watchers.Add(constraint);
        constraint.Watched2.Watchers.Add(constraint);
        constraint.IsAdditional = constraint.IsLearned = true;

        var unitConstraint = new Constraint([store.Variables[0].NegativeLiteral],
            store.Variables[0].NegativeLiteral, store.Variables[0].NegativeLiteral);
        unitConstraint.Watched1.Watchers.Add(unitConstraint);

        unitsToPropagate.Enqueue((store.Variables[1].NegativeLiteral, constraint));
        var sut = Create(store);

        var seq = new MockSequence();
        trail.InSequence(seq).Setup(t => t.Reset());
        constraintFactory.InSequence(seq).Setup(cf => cf.ReleaseConstraint(constraint));

        sut.Reset(true);

        Assert.Equal([(store.Variables[0].NegativeLiteral, unitConstraint)], unitsToPropagate);
        trail.VerifyAll();
        constraintFactory.VerifyAll();
    }

    [Fact]
    public void AddClause_Canceled_OperationCanceledException()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        using var cts = new CancellationTokenSource();
        var store = new TestComponentStore(new(), 0, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            _ => null!
        }, cts.Token);

        var sut = Create(store);
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => sut.AddClause(new Clause([])));
    }
    [Fact]
    public void AddClause_Null_ArgumentNullException()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var store = new TestComponentStore(new(), 0, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            _ => null!
        });

        var sut = Create(store);
        Assert.Throws<ArgumentNullException>(() => sut.AddClause(null!));
    }
    [Fact]
    public void AddClause_EmptyClause_ArgumentException()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var store = new TestComponentStore(new(), 0, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            _ => null!
        });

        var sut = Create(store);
        Assert.Throws<ArgumentException>(() => sut.AddClause(new Clause([])));
    }
    [Fact]
    public void AddClause_InvalidLiterals_ArgumentException()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var store = new TestComponentStore(new(), 10, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            _ => null!
        });

        var sut = Create(store);
        Assert.Throws<ArgumentException>(() => sut.AddClause(new Clause([1, 2, 11])));
    }
    [Fact]
    public void AddClause_AtInitialState()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var store = new TestComponentStore(new(), 10, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.ConstraintFactory) => constraintFactory.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var literals = store.Literals;
        var clause = new Clause([1, -2, 3]);
        var expectedLiterals = new[] { literals[0], literals[3], literals[4] };
        var constraint = new Constraint(expectedLiterals, expectedLiterals[0], expectedLiterals[1]);

        var seq = new MockSequence();
        constraintFactory.InSequence(seq).Setup(cf =>
            cf.CreateAdditionalConstraint(It.Is<IEnumerable<ConstraintLiteral>>(literals => literals.SequenceEqual(expectedLiterals))))
            .Returns(constraint);
        trail.InSequence(seq).Setup(t => t.Count).Returns(0);

        var units = store.UnitPropagationQueue;
        var sut = Create(store);
        sut.AddClause(clause);

        Assert.Empty(units);
        trail.VerifyAll();
        constraintFactory.VerifyAll();
    }
    [Fact]
    public void AddClause_Fulfilled()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var store = new TestComponentStore(new(), 10, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.ConstraintFactory) => constraintFactory.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var literals = store.Literals;
        var clause = new Clause([1, -2, 3]);
        var expectedLiterals = new[] { literals[0], literals[3], literals[4] };
        literals[0].Sense = true;
        var constraint = new Constraint(expectedLiterals, literals[0], expectedLiterals[1]);

        var seq = new MockSequence();
        constraintFactory.InSequence(seq).Setup(cf =>
            cf.CreateAdditionalConstraint(It.Is<IEnumerable<ConstraintLiteral>>(literals => literals.SequenceEqual(expectedLiterals))))
            .Returns(constraint);
        trail.InSequence(seq).Setup(t => t.Count).Returns(1);

        var units = store.UnitPropagationQueue;
        var sut = Create(store);
        sut.AddClause(clause);

        Assert.Empty(units);
        trail.VerifyAll();
        constraintFactory.VerifyAll();
    }
    [Fact]
    public void AddClause_AllAssigned_ByLevel0()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var store = new TestComponentStore(new(), 10, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.ConstraintFactory) => constraintFactory.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var literals = store.Literals;
        var clause = new Clause([1, -2, 3]);
        var expectedLiterals = new[] { literals[0], literals[3], literals[4] };
        expectedLiterals[0].Sense = false;
        expectedLiterals[0].Variable.DecisionLevel = 0;
        expectedLiterals[1].Sense = false;
        expectedLiterals[1].Variable.DecisionLevel = 0;
        expectedLiterals[2].Sense = false;
        expectedLiterals[2].Variable.DecisionLevel = 0;
        var constraint = new Constraint(expectedLiterals, literals[0], expectedLiterals[1]);

        var seq = new MockSequence();
        constraintFactory.InSequence(seq).Setup(cf =>
            cf.CreateAdditionalConstraint(It.Is<IEnumerable<ConstraintLiteral>>(literals => literals.SequenceEqual(expectedLiterals))))
            .Returns(constraint);
        trail.InSequence(seq).Setup(t => t.Count).Returns(1);
        trail.InSequence(seq).Setup(t => t.Reset());

        var units = store.UnitPropagationQueue;
        units.Enqueue((literals[0], constraint));
        var sut = Create(store);
        sut.AddClause(clause);

        Assert.Empty(units);
        trail.VerifyAll();
        constraintFactory.VerifyAll();
    }
    [Fact]
    public void AddClause_AllAssigned_JumpBackAndConflict()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var conflictHandler = new Mock<IHandleConflicts>(MockBehavior.Strict);
        var store = new TestComponentStore(new(), 10, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.ConstraintFactory) => constraintFactory.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            nameof(ComponentStoreBase.ConflictHandler) => conflictHandler.Object,
            _ => null!
        });

        const int decisionLevel = 5;
        var literals = store.Literals;
        var clause = new Clause([1, -2, 3]);
        var expectedLiterals = new[] { literals[0], literals[3], literals[4] };
        expectedLiterals[0].Sense = false;
        expectedLiterals[0].Variable.DecisionLevel = decisionLevel;
        expectedLiterals[1].Sense = false;
        expectedLiterals[1].Variable.DecisionLevel = 4;
        expectedLiterals[2].Sense = false;
        expectedLiterals[2].Variable.DecisionLevel = 3;
        var constraint = new Constraint(expectedLiterals, expectedLiterals[0], expectedLiterals[1]);
        
        var seq = new MockSequence();
        constraintFactory.InSequence(seq).Setup(cf =>
            cf.CreateAdditionalConstraint(It.Is<IEnumerable<ConstraintLiteral>>(literals => literals.SequenceEqual(expectedLiterals))))
            .Returns(constraint);
        trail.InSequence(seq).Setup(t => t.Count).Returns(12);
        trail.InSequence(seq).Setup(t => t.JumpBack(decisionLevel));
        conflictHandler.InSequence(seq).Setup(c => c.HandleConflict(constraint));

        var sut = Create(store);
        sut.AddClause(clause);

        trail.VerifyAll();
        constraintFactory.VerifyAll();
        conflictHandler.VerifyAll();
    }
    [Fact]
    public void AddClause_Unassigned_Single()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var store = new TestComponentStore(new(), 10, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.ConstraintFactory) => constraintFactory.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var literals = store.Literals;
        var clause = new Clause([1]);
        var expectedLiterals = new[] { literals[0] };
        var constraint = new Constraint(expectedLiterals, expectedLiterals[0], expectedLiterals[0]);

        var seq = new MockSequence();
        constraintFactory.InSequence(seq).Setup(cf =>
            cf.CreateAdditionalConstraint(It.Is<IEnumerable<ConstraintLiteral>>(literals => literals.SequenceEqual(expectedLiterals))))
            .Returns(constraint);
        trail.InSequence(seq).Setup(t => t.Count).Returns(12);

        var units = store.UnitPropagationQueue;
        var sut = Create(store);
        sut.AddClause(clause);

        Assert.Equal([(literals[0], constraint)], units);

        trail.VerifyAll();
        constraintFactory.VerifyAll();
    }
    [Fact]
    public void AddClause_Unassigned_Unit()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var store = new TestComponentStore(new(), 10, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.ConstraintFactory) => constraintFactory.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var literals = store.Literals;
        var clause = new Clause([1, 2]);
        var expectedLiterals = new[] { literals[0], literals[2] };
        var constraint = new Constraint(expectedLiterals, expectedLiterals[0], expectedLiterals[1]);
        expectedLiterals[1].Sense = false;
        var seq = new MockSequence();
        constraintFactory.InSequence(seq).Setup(cf =>
            cf.CreateAdditionalConstraint(It.Is<IEnumerable<ConstraintLiteral>>(literals => literals.SequenceEqual(expectedLiterals))))
            .Returns(constraint);
        trail.InSequence(seq).Setup(t => t.Count).Returns(12);

        var units = store.UnitPropagationQueue;
        var sut = Create(store);
        sut.AddClause(clause);

        Assert.Equal([(literals[0], constraint)], units);

        trail.VerifyAll();
        constraintFactory.VerifyAll();
    }
    [Fact]
    public void AddClause_AllUnassigned()
    {
        var preProcessor = new Mock<IPreProcessor>();
        var heap = new Mock<ICandidateHeap>();
        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var store = new TestComponentStore(new(), 10, name => name switch
        {
            nameof(ComponentStoreBase.CandidateHeap) => heap.Object,
            nameof(ComponentStoreBase.PreProcessor) => preProcessor.Object,
            nameof(ComponentStoreBase.ConstraintFactory) => constraintFactory.Object,
            nameof(ComponentStoreBase.VariableTrail) => trail.Object,
            _ => null!
        });

        var literals = store.Literals;
        var clause = new Clause([1, 2]);
        var expectedLiterals = new[] { literals[0], literals[2] };
        var constraint = new Constraint(expectedLiterals, expectedLiterals[0], expectedLiterals[1]);
        var seq = new MockSequence();
        constraintFactory.InSequence(seq).Setup(cf =>
            cf.CreateAdditionalConstraint(It.Is<IEnumerable<ConstraintLiteral>>(literals => literals.SequenceEqual(expectedLiterals))))
            .Returns(constraint);
        trail.InSequence(seq).Setup(t => t.Count).Returns(12);

        var units = store.UnitPropagationQueue;
        var sut = Create(store);
        sut.AddClause(clause);

        Assert.Empty(units);

        trail.VerifyAll();
        constraintFactory.VerifyAll();
    }
}