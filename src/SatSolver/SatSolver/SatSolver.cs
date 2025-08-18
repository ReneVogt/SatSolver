using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
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
    readonly RestartManager _restartManager;
    readonly CancellationToken _cancellationToken;
    readonly ICandidateHeap _candidateHeap;
    readonly IVariableTrail _trail;
    readonly IVariablePropagator _dpllProcessor;
    readonly IConflictDrivenClauseLearner _cdclProcessor;
    readonly IActivityManager _activityManager;
    readonly bool _onlyPoorMansVSIDS;
    readonly Queue<(ConstraintLiteral UnitLiteral, Constraint Reason)> _unitsToPropagate;
    readonly PropagationRateTracker _propagationRateTracker;
    readonly IReduceLearnedConstraints _learnedConstraintsReducer;
    readonly Variable[] _variables;


    SatSolver(IInitializeSatSolver initializer)
    {
        var state = initializer.Initialize();
        _dpllProcessor = state.VariablePropagator;
        _cdclProcessor = state.CdclProcessor;
        _activityManager = state.ActivityManager;
        _trail = state.VariableTrail;
        _candidateHeap = state.CandidateHeap;
        _restartManager = state.RestartManager;
        _unitsToPropagate = state.UnitsToPropagate;
        _propagationRateTracker = state.PropagationRateTracker;
        _restartManager = state.RestartManager;
        _cancellationToken = state.CancellationToken;
        _onlyPoorMansVSIDS = state.Options.OnlyPoorMansVSIDS;
        _variables = state.Variables;
        _learnedConstraintsReducer = state.LearnedConstraintsReducer;
    }

    Literal[]? Solve()
    {
        var propagationCount = 0;
        if (_dpllProcessor.PropagateUnits(ref propagationCount) is not null)
            return null;
        _trail.Clear();
        return _onlyPoorMansVSIDS ? SolvePoor() : SolveCDCL();
    }

    Literal[]? SolvePoor()
    {
        Variable? candidateVariable = null;
        var candidateSense = true;

        for(; ; )
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var firstTry = false;

            if (candidateVariable is null)
            {
                candidateVariable = _candidateHeap.Dequeue();
                if (candidateVariable is null) return BuildSolution();
                candidateSense = candidateVariable.Polarity;
                firstTry = true;
            }

            _trail.Push(firstTry);
            Debug.WriteLine($"[{_trail.DecisionLevel}] Decided {candidateVariable.Index+1} to {candidateSense}.");
            var conflictingConstraint = _dpllProcessor.PropagateVariable(candidateVariable, candidateSense, null, out var propagationCount);
            conflictingConstraint ??= _dpllProcessor.PropagateUnits(ref propagationCount);

            _propagationRateTracker.AddPropagations(propagationCount);

            candidateVariable = null;
            if (conflictingConstraint is null) continue;

            Debug.WriteLine($"Conflict in {conflictingConstraint}");
            _propagationRateTracker.AddConflict();
            _restartManager.AddConflict();
            _activityManager.IncreaseVariableActivity(conflictingConstraint);

            if (_restartManager.RestartIfNecessary()) continue;
            
            Debug.WriteLine("Backtracking.");
            _unitsToPropagate.Clear();
            (candidateVariable, candidateSense) = _trail.Backtrack();
            if (candidateVariable is null) return null;
        }
    }
    Literal[]? SolveCDCL()
    {
        Variable? candidateVariable = null;
        Constraint? learnedConstraint = null;
        var candidateSense = true;

        for (;;)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (candidateVariable is null)
            {
                candidateVariable = _candidateHeap.Dequeue();
                if (candidateVariable is null) return BuildSolution();
                candidateSense = candidateVariable.Polarity;
                _trail.Push();
                Debug.WriteLine($"[{_trail.DecisionLevel}] Decided {candidateVariable.Index+1} to {candidateSense}.");
            }

            var conflictingConstraint = 
                _dpllProcessor.PropagateVariable(candidateVariable, candidateSense, learnedConstraint, out var propagationCount) ??
                _dpllProcessor.PropagateUnits(ref propagationCount);

            _propagationRateTracker.AddPropagations(propagationCount);

            candidateVariable = null;
            learnedConstraint = null;
            if (conflictingConstraint is null) continue;
            Debug.WriteLine($"Conflict in {conflictingConstraint} (learned: {conflictingConstraint.IsLearned}).");
            if (_trail.DecisionLevel == 0) return null;

            _propagationRateTracker.AddConflict();
            _restartManager.AddConflict();
            _activityManager.IncreaseConstraintActivity(conflictingConstraint);
            _unitsToPropagate.Clear();

            var (candidateLiteral, candidateReason) = _cdclProcessor.PerformClauseLearning(conflictingConstraint);
            _learnedConstraintsReducer.ReduceLearnedConstraintsIfNecessary();
            if (_restartManager.RestartIfNecessary()) continue;

            candidateVariable = candidateLiteral.Variable;
            candidateSense = candidateLiteral.Orientation;
            learnedConstraint = candidateReason;
            Debug.WriteLine($"[{_trail.DecisionLevel}] Propagating uip {candidateVariable.Index+1} to {candidateSense}.");
        }
    }

    Literal[] BuildSolution() => [.. _variables.Select(v => new Literal(v.Index+1, v.Sense!.Value))];

    // This is the entry point for unit tests. We can provide an alternative initializer
    // and via that mocks for all the required algorithm parts.
    static Literal[]? Solve(IInitializeSatSolver initializer) => new SatSolver(initializer).Solve();

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
    public static Literal[]? Solve(Problem problem, SatSolverOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = problem ?? throw new ArgumentNullException(nameof(problem));
        if (problem.Clauses.Any(clause => clause.Literals.Length == 0)) return null;
        if (problem.NumberOfLiterals == 0) return [];
        if (problem.Clauses.Length == 0) return [.. Enumerable.Range(1, problem.NumberOfLiterals).Select(i => new Literal(i, true))];
        return Solve(new SatSolverInitializer(problem, options ?? SatSolverOptions.Default, cancellationToken));
    }
}
