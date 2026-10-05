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
