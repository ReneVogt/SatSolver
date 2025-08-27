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
sealed partial class SatSolverInternal<
    TCandidateHeap,
    TVariableTrail,
    TVariablePropagator,
    TConflictHandler,
    TActivityManager,
    TPropagationRateTracker,
    TLearnedConstraintsReducer,
    TRestartManager>
    where TCandidateHeap : ICandidateHeap
    where TVariableTrail : IVariableTrail
    where TVariablePropagator : IPropagateVariables
    where TConflictHandler : IHandleConflicts
    where TActivityManager : IManageActivities
    where TPropagationRateTracker : ITrackPropagationRate
    where TLearnedConstraintsReducer : IReduceLearnedConstraints
    where TRestartManager : IManageRestart
{
    readonly ComponentStore _store;
    readonly TRestartManager _restartManager;
    readonly CancellationToken _cancellationToken;
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

    public SatSolverInternal(IInitializeSatSolver initializer)
    {
        _store = initializer.Initialize();
        Statistics.Initialize(_store.PropagationRateTracker, _store.LiteralBlockDistanceTracker);
        _variablePropagator = (TVariablePropagator)_store.VariablePropagator;
        _conflictHandler = (TConflictHandler)_store.ConflictHandler;
        _activityManager = (TActivityManager)_store.ActivityManager;
        _trail = (TVariableTrail)_store.VariableTrail;
        _candidateHeap = (TCandidateHeap)_store.CandidateHeap;
        _restartManager = (TRestartManager)_store.RestartManager;
        _unitPropagationQueue = _store.UnitPropagationQueue;
        _propagationRateTracker = (TPropagationRateTracker)_store.PropagationRateTracker;
        _restartManager = (TRestartManager)_store.RestartManager;
        _cancellationToken = _store.CancellationToken;
        _dpllOnly = _store.Options.Mode == SatSolverMode.DPLL;
        _variables = _store.Variables;
        _learnedConstraintsReducer = (TLearnedConstraintsReducer)_store.LearnedConstraintsReducer;
    }

    public IEnumerable<Literal[]> EnumerateSolutions()
    {
        while (_unitPropagationQueue.Count > 0)
        {
            var (literal, reason) = _unitPropagationQueue.Dequeue();
            if (literal.Sense is not null) continue;
            if (_variablePropagator.PropagateVariable(literal.Variable, literal.Orientation, reason) is not null)
                yield break;
        }

        _trail.Clear();
        var solutions = _dpllOnly ? SolveDPLL() : SolveCDCL();
        foreach(var solution in solutions)
            yield return solution;
    }

    IEnumerable<Literal[]> SolveDPLL()
    {
        Variable? candidateVariable = null;
        var candidateSense = true;

        for(; ; )
        {
            Constraint? conflictingConstraint = null;

            _cancellationToken.ThrowIfCancellationRequested();

            var firstTry = false;

            if (candidateVariable is null)
            {
                candidateVariable = _candidateHeap.Dequeue();
                if (candidateVariable is null)
                {
                    var solution = BuildSolution();
                    Debug.WriteLine($"Delivering solution [{string.Join(" ", solution.AsEnumerable())}] and creating inverse conflict.");
                    yield return solution;
                    conflictingConstraint = CreateConflictFromSolution();
                }
                else
                {
                    candidateSense = candidateVariable.Polarity;
                    firstTry = true;
                }
            }

            if (conflictingConstraint is null)
            {
                _trail.Push(firstTry);
                Debug.WriteLine($"[{_trail.DecisionLevel}] Decided {candidateVariable!.Index+1} to {candidateSense}.");
                conflictingConstraint = _variablePropagator.PropagateVariable(candidateVariable, candidateSense, null);
                while (conflictingConstraint is null && _unitPropagationQueue.Count > 0)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    (var literal, _) = _unitPropagationQueue.Dequeue();
                    if (literal.Sense is not null) continue;
                    Debug.WriteLine($"[{_trail.DecisionLevel}] Propagating {literal.Variable.Index+1} to {literal.Orientation}.");
                    conflictingConstraint = _variablePropagator.PropagateVariable(literal.Variable, literal.Orientation, null);
                }
            }

            candidateVariable = null;
            if (conflictingConstraint is null) continue;

            Debug.WriteLine($"Conflict in {conflictingConstraint}");
            _propagationRateTracker.AddConflict();
            _restartManager.AddConflict();
            _activityManager.IncreaseVariableActivity(conflictingConstraint);

            if (_restartManager.RestartIfNecessary()) continue;
            
            Debug.WriteLine("Backtracking.");
            _unitPropagationQueue.Clear();
            (candidateVariable, candidateSense) = _trail.Backtrack();
            if (candidateVariable is null) yield break;
        }
    }
    IEnumerable<Literal[]> SolveCDCL()
    {
        for (;;)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _unitPropagationQueue.Clear();

            var candidateVariable = _candidateHeap.Dequeue();
            if (candidateVariable is not null)
                _unitPropagationQueue.Enqueue((candidateVariable.Polarity ? candidateVariable.PositiveLiteral : candidateVariable.NegativeLiteral, null));
            else
            {
                var solution = BuildSolution();
                Debug.WriteLine($"Delivering solution [{string.Join(" ", solution.AsEnumerable())}] and creating inverse conflict.");
                yield return solution;
                _conflictHandler.HandleConflict(CreateConflictFromSolution());
            }

            while (_unitPropagationQueue.Count > 0)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var (unitLiteral, reason) = _unitPropagationQueue.Dequeue();
                if (unitLiteral.Sense is not null) continue;

                if (reason is null)
                {
                    _trail.Push();
                    Debug.WriteLine($"[{_trail.DecisionLevel}] Decided {unitLiteral.Variable.Index+1} to {unitLiteral.Orientation}.");
                }
                else
                    Debug.WriteLine($"[{_trail.DecisionLevel}] Propagating {unitLiteral.Variable.Index+1} to {unitLiteral.Orientation}.");


                var conflictingConstraint = _variablePropagator.PropagateVariable(unitLiteral.Variable, unitLiteral.Orientation, reason);
                if (conflictingConstraint is null) continue;

                Debug.WriteLine($"Conflict in {conflictingConstraint} (learned: {conflictingConstraint.IsLearned}).");
                if (_trail.DecisionLevel == 0) yield break;
                _conflictHandler.HandleConflict(conflictingConstraint);

                Statistics.Dump();
                _learnedConstraintsReducer.ReduceLearnedConstraintsIfNecessary();
                if (_restartManager.RestartIfNecessary()) break;
            }
        }
    }
    Literal[] BuildSolution() => [.. _variables.Select(v => new Literal(v.Index+1, v.Sense!.Value))];

    Constraint CreateConflictFromSolution()
    {
        var literals = _variables.Select(variable => variable.Sense!.Value ? variable.NegativeLiteral : variable.PositiveLiteral);
        var trailedVariable = _trail[^1];
        var firstWatched = trailedVariable.Sense!.Value ? trailedVariable.NegativeLiteral : trailedVariable.PositiveLiteral;
        var secondWatched = firstWatched;
        if (_trail.Count > 1)
        {
            trailedVariable = _trail[^2];
            secondWatched = trailedVariable.Sense!.Value ? trailedVariable.NegativeLiteral : trailedVariable.PositiveLiteral;
        }
        return new(literals, firstWatched, secondWatched) { Activity = _activityManager.ConstraintActivityIncrement };
    } 
}
