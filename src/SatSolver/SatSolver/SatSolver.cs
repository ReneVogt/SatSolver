using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;
using System.Diagnostics;

namespace Revo.SatSolver;

/// <summary>
/// Finds a variable configuration that 
/// satisfies all clauses in a SATisfiability 
/// problem.
/// </summary>
sealed partial class SatSolver : ISatSolver
{
    readonly IManageRestart _restartManager;
    readonly IConstraintFactory _constraintFactory;
    readonly ICandidateHeap _candidateHeap;
    readonly IVariableTrail _trail;
    readonly IPropagateVariables _variablePropagator;
    readonly IHandleConflicts _conflictHandler;
    readonly IManageActivities _activityManager;
    readonly UnitPropagationQueue _unitPropagationQueue;
    readonly ITrackPropagationRate _propagationRateTracker;
    readonly IReduceLearnedConstraints _learnedConstraintsReducer;
    readonly Variable[] _variables;
    readonly ConstraintLiteral[] _literals;

    int _originalConstraintCount;
    bool _isUnsatisfiable;

    public SatSolver(ComponentStoreBase store)
    {
        _constraintFactory = store.ConstraintFactory;
        _variablePropagator = store.VariablePropagator;
        _conflictHandler = store.ConflictHandler;
        _activityManager = store.ActivityManager;
        _trail = store.VariableTrail;
        _candidateHeap = store.CandidateHeap;
        _restartManager = store.RestartManager;
        _unitPropagationQueue = store.UnitPropagationQueue;
        _propagationRateTracker = store.PropagationRateTracker;
        _variables = store.Variables;
        _literals = store.Literals;
        _learnedConstraintsReducer = store.LearnedConstraintsReducer;

        _originalConstraintCount = store.PreProcessor.BuildConstraints();
        _candidateHeap.Heapify();
    }

    public Literal[]? FindSolution(CancellationToken cancellationToken = default) => Solve(cancellationToken);

    Literal[]? Solve(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_isUnsatisfiable) return null;

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
                if (unitLiteral.Sense is not null)
                {
                    // A contradictory assignment must already have caused a conflict
                    // during propagation; only fulfilled queue entries may be skipped.
                    Debug.Assert(unitLiteral.Sense == true);
                    continue;
                }

                if (reason is null)
                    _trail.Push();

                var conflictingConstraint = _variablePropagator.PropagateVariable(unitLiteral.Variable, unitLiteral.Orientation, reason);
                if (conflictingConstraint is null) continue;

                if (_trail.DecisionLevel == 0)
                {
                    _isUnsatisfiable = true;
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

        if (_isUnsatisfiable) return;
        if (constraint.Watched1.Sense == false)
        {
            var level = constraint.Watched1.Variable.DecisionLevel;
            if (level == 0)
            {
                _isUnsatisfiable = true;
                Statistics.NoMoreSolutions();
                return;
            }
            
            if (ResetIfUnitsPending()) return;
            _trail.JumpBack(level);
            _conflictHandler.HandleConflict(constraint);
            return;
        }

        // Two non-false watchers remain safe across backjumps.
        if (constraint.Literals.Length > 1 && constraint.Watched2.Sense != false) return;

        var unitLevel = constraint.Literals.Length == 1 ? 0 : constraint.Watched2.Variable.DecisionLevel;
        var unitLiteral = constraint.Watched1;
        if (unitLiteral.Sense == true && unitLiteral.Variable.DecisionLevel <= unitLevel) return;

        // The clause is unit at this level, even if its only non-false literal
        // is currently true at a higher level. Establish its implication before
        // a later backjump or restart can silently remove that assignment.
        if (_trail.DecisionLevel > unitLevel)
        {
            if (ResetIfUnitsPending()) return;
            _trail.JumpBack(unitLevel);
        }
        Debug.Assert(unitLiteral.Sense is null);
        _unitPropagationQueue.Enqueue((unitLiteral, constraint));
    }
    bool ResetIfUnitsPending()
    {
        if (_unitPropagationQueue.Count == 0) return false;

        // A backjump may invalidate queued reasons; simply clearing the queue
        // can lose implications of earlier additions. Replay from unassigned
        // variables, retaining every clause (including the new one).
        Reset();
        return true;
    }
    public void Reset(bool removeAdditionalClauses = false)
    {
        _trail.Reset();

        if (removeAdditionalClauses)
        {
            _constraintFactory.ReleaseAdditionalConstraints();
            _isUnsatisfiable = false;
        }

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
