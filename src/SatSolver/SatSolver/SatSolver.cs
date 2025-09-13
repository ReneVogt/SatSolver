using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace Revo.SatSolver;

/// <summary>
/// Finds a variable configuration that 
/// satisfies all clauses in a SATisfiability 
/// problem.
/// </summary>
sealed partial class SatSolver<
    TConstraintFactory,
    TCandidateHeap,
    TVariableTrail,
    TVariablePropagator,
    TConflictHandler,
    TActivityManager,
    TPropagationRateTracker,
    TLearnedConstraintsReducer,
    TRestartManager> : ISatSolver
    where TConstraintFactory : IConstraintFactory
    where TCandidateHeap : ICandidateHeap
    where TVariableTrail : IVariableTrail
    where TVariablePropagator : IPropagateVariables
    where TConflictHandler : IHandleConflicts
    where TActivityManager : IManageActivities
    where TPropagationRateTracker : ITrackPropagationRate
    where TLearnedConstraintsReducer : IReduceLearnedConstraints
    where TRestartManager : IManageRestart
{
    readonly TRestartManager _restartManager;
    readonly TConstraintFactory _constraintFactory;
    readonly TCandidateHeap _candidateHeap;
    readonly TVariableTrail _trail;
    readonly TVariablePropagator _variablePropagator;
    readonly TConflictHandler _conflictHandler;
    readonly TActivityManager _activityManager;
    readonly bool _dpllOnly;
    readonly UnitPropagationQueue _unitPropagationQueue;
    readonly TPropagationRateTracker _propagationRateTracker;
    readonly TLearnedConstraintsReducer _learnedConstraintsReducer;
    readonly Variable[] _variables;
    readonly ConstraintLiteral[] _literals;

    int _originalConstraintCount;

    public SatSolver(ComponentStoreBase store)
    {
        _constraintFactory = (TConstraintFactory)store.ConstraintFactory;
        _variablePropagator = (TVariablePropagator)store.VariablePropagator;
        _conflictHandler = (TConflictHandler)store.ConflictHandler;
        _activityManager = (TActivityManager)store.ActivityManager;
        _trail = (TVariableTrail)store.VariableTrail;
        _candidateHeap = (TCandidateHeap)store.CandidateHeap;
        _restartManager = (TRestartManager)store.RestartManager;
        _unitPropagationQueue = store.UnitPropagationQueue;
        _propagationRateTracker = (TPropagationRateTracker)store.PropagationRateTracker;
        _restartManager = (TRestartManager)store.RestartManager;
        _dpllOnly = store.Options.Mode == SatSolverMode.DPLL;
        _variables = store.Variables;
        _literals = store.Literals;
        _learnedConstraintsReducer = (TLearnedConstraintsReducer)store.LearnedConstraintsReducer;

        _originalConstraintCount = store.PreProcessor.BuildConstraints();
        _candidateHeap.Heapify();
    }

    public Literal[]? FindSolution(CancellationToken cancellationToken = default) => _dpllOnly ? SolveDPLL(cancellationToken) : SolveCDCL(cancellationToken);

    Literal[]? SolveDPLL(CancellationToken cancellationToken)
    {
        Variable? candidateVariable = null;
        var candidateSense = true;

        for(; ; )
        {
            cancellationToken.ThrowIfCancellationRequested();

            var firstTry = false;
            Constraint? conflictingConstraint = null;

            if (_unitPropagationQueue.Count == 0)
            {
                if (candidateVariable is null)
                {
                    candidateVariable = _candidateHeap.Dequeue();
                    if (candidateVariable is null)
                    {
                        var solution = BuildSolution();
                        Statistics.DeliveringSolution(solution);
                        return solution;
                    }
                    else
                    {
                        candidateSense = candidateVariable.Polarity;
                        firstTry = true;
                    }
                }

                _trail.Push(firstTry);
                conflictingConstraint = _variablePropagator.PropagateVariable(candidateVariable, candidateSense, null);
            }

            while (conflictingConstraint is null && _unitPropagationQueue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                (var literal, _) = _unitPropagationQueue.Dequeue();
                if (literal.Sense is not null)
                {
                    Debug.Assert(literal.Sense.Value);
                    continue;
                }
                conflictingConstraint = _variablePropagator.PropagateVariable(literal.Variable, literal.Orientation, null);
            }

            candidateVariable = null;
            if (conflictingConstraint is null) continue;

            Statistics.AddConflict(conflictingConstraint);
            _propagationRateTracker.AddConflict();
            _restartManager.AddConflict();
            _activityManager.IncreaseVariableActivity(conflictingConstraint);

            if (_restartManager.RestartIfNecessary()) continue;
            
            _unitPropagationQueue.Clear();
            (candidateVariable, candidateSense) = _trail.Backtrack();
            if (candidateVariable is null) return null;
        }
    }
    Literal[]? SolveCDCL(CancellationToken cancellationToken)
    {
        for (;;)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_unitPropagationQueue.Count == 0)
            {
                var candidateVariable = _candidateHeap.Dequeue();
                if (candidateVariable is not null)
                    _unitPropagationQueue.Enqueue((candidateVariable.Polarity ? candidateVariable.PositiveLiteral : candidateVariable.NegativeLiteral, null));
                else
                {
                    var solution = BuildSolution();
                    Statistics.DeliveringSolution(solution);
                    return solution;
                }
            }

            while (_unitPropagationQueue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (unitLiteral, reason) = _unitPropagationQueue.Dequeue();
                if (unitLiteral.Sense is not null) continue;

                if (reason is null)
                    _trail.Push();

                var conflictingConstraint = _variablePropagator.PropagateVariable(unitLiteral.Variable, unitLiteral.Orientation, reason);
                if (conflictingConstraint is null) continue;

                if (_trail.DecisionLevel == 0)
                {
                    Statistics.NoMoreSolutions();
                    return null;
                }
                _conflictHandler.HandleConflict(conflictingConstraint);

                _learnedConstraintsReducer.ReduceLearnedConstraintsIfNecessary(_originalConstraintCount);
                if (_restartManager.RestartIfNecessary()) break;
            }
        }
    }
    Literal[] BuildSolution() => [.. _variables.Select(v => new Literal(v.Index+1, v.Sense!.Value))];
    
    public void AddClause(Clause clause)
    {
        _ = clause ?? throw new ArgumentNullException(nameof(clause));
        if (clause.Literals.Length == 0)
            throw new ArgumentException(paramName: nameof(clause), message: "Empty clauses are not supported.");
        if (clause.Literals.Any(l => l.Id > _variables.Length))
            throw new ArgumentException(paramName: nameof(clause), message: "Clause contains invalid literals.");

        _originalConstraintCount++;

        var constraint = _constraintFactory.CreateAdditionalConstraint(
            clause.Literals.Select(l => l.Sense ? _variables[l.Id-1].PositiveLiteral : _variables[l.Id-1].NegativeLiteral));

        if (constraint.Watched1.Sense == true) return;
        if (constraint.Watched1.Sense is not null)
        {
            var level = constraint.Watched1.Variable.DecisionLevel;
            if (level == 0)
            {
                _trail.Reset();
                SetInitialUnits();
                return;
            }
            
            if (_dpllOnly)
            {
                _unitPropagationQueue.Clear();
                _trail.JumpBack(level - 1);
                if (constraint.Literals.Length == 1 || constraint.Watched2.Sense is not null)
                    _unitPropagationQueue.Enqueue((constraint.Watched1, constraint));
                return;
            }

            _trail.JumpBack(level);
            _conflictHandler.HandleConflict(constraint);
            return;
        }

        if (constraint.Literals.Length == 1 || constraint.Watched2.Sense is not null)
            _unitPropagationQueue.Enqueue((constraint.Watched1, constraint));
    }
    public void Reset(bool removeAdditionalClauses = false)
    {
        _trail.Reset();

        if (removeAdditionalClauses)
            _constraintFactory.ReleaseAdditionalConstraints();

        SetInitialUnits();
    }
    void SetInitialUnits()
    {
        _unitPropagationQueue.Clear();
        var units = _literals.SelectMany(literal => literal.Watchers)
            .Where(watcher => watcher.Literals.Length == 1);
        foreach (var unit in units) _unitPropagationQueue.Enqueue((unit.Literals[0], unit));
    }
}
