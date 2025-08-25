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
public sealed partial class SatSolver
{
    readonly ComponentStore _store;
    readonly IManageRestart _restartManager;
    readonly CancellationToken _cancellationToken;
    readonly ICandidateHeap _candidateHeap;
    readonly IVariableTrail _trail;
    readonly IPropagateVariables _variablePropagator;
    readonly IHandleConflicts _conflictHandler;
    readonly IManageActivities _activityManager;
    readonly bool _onlyPoorMansVSIDS;
    readonly UnitPropagationQueue _unitPropagationQueue;
    readonly ITrackPropagationRate _propagationRateTracker;
    readonly IReduceLearnedConstraints _learnedConstraintsReducer;
    readonly Variable[] _variables;


    SatSolver(IInitializeSatSolver initializer)
    {
        _store = initializer.Initialize();
        _variablePropagator = _store.VariablePropagator;
        _conflictHandler = _store.ConflictHandler;
        _activityManager = _store.ActivityManager;
        _trail = _store.VariableTrail;
        _candidateHeap = _store.CandidateHeap;
        _restartManager = _store.RestartManager;
        _unitPropagationQueue = _store.UnitPropagationQueue;
        _propagationRateTracker = _store.PropagationRateTracker;
        _restartManager = _store.RestartManager;
        _cancellationToken = _store.CancellationToken;
        _onlyPoorMansVSIDS = _store.Options.OnlyPoorMansVSIDS;
        _variables = _store.Variables;
        _learnedConstraintsReducer = _store.LearnedConstraintsReducer;
    }

    IEnumerable<Literal[]> EnumerateSolutions()
    {
        while (_unitPropagationQueue.Count > 0)
        {
            var (literal, reason) = _unitPropagationQueue.Dequeue();
            if (literal.Sense is not null) continue;
            if (_variablePropagator.PropagateVariable(literal.Variable, literal.Orientation, reason) is not null)
                yield break;
        }

        _trail.Clear();
        var solutions = _onlyPoorMansVSIDS ? SolvePoor() : SolveCDCL();
        foreach(var solution in solutions)
            yield return solution;
    }

    IEnumerable<Literal[]> SolvePoor()
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
                    Debug.WriteLine($"Delivering solution {solution} and creating inverse conflict.");
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
                Debug.WriteLine($"Delivering solution {solution} and creating inverse conflict.");
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
        return new(literals, firstWatched, secondWatched);
    } 


    // This is the entry point for unit tests. We can provide an alternative initializer
    // and via that mocks for all the required algorithm parts.
    internal static IEnumerable<Literal[]> EnumerateSolutions(IInitializeSatSolver initializer) => new SatSolver(initializer).EnumerateSolutions();

    /// <summary>
    /// Finds a variable configuration that satisfies the SATisfiability <paramref name="problem"/>.
    /// If there is no solution the method return, <c>null</c>.
    /// </summary>
    /// <param name="problem">The <see cref="Problem"/> to satisfy.</param>
    /// <param name="options">The options for the solver.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>If a solution was found the method returns an array of <see cref="Literal"/>s indicating
    /// their senses that solve the problem. If no solution was found the method returns <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> was <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The problem contains either invalid literal IDs or no literals at all.</exception>
    public static IEnumerable<Literal[]> EnumerateSolutions(Problem problem, SatSolverOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = problem ?? throw new ArgumentNullException(nameof(problem));
        if (problem.Clauses.Any(clause => clause.Literals.Length == 0)) return [];
        if (problem.NumberOfLiterals == 0) return [[]];
        return EnumerateSolutions(new SatSolverInitializer(problem, options ?? SatSolverOptions.Default, cancellationToken));
    }
}
