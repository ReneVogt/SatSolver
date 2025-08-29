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
public static class SatSolver
{
    // This is the entry point for unit tests. We can provide an alternative initializer
    // and via that mocks for all the required algorithm parts.
    internal static IEnumerable<Literal[]> EnumerateSolutions(IInitializeSatSolver initializer) => new SatSolverInternal<
        IConstraintFactory,
        ICandidateHeap,
        IVariableTrail,
        IPropagateVariables,
        IHandleConflicts,
        IManageActivities,
        ITrackPropagationRate,
        IReduceLearnedConstraints,
        IManageRestart>
        (initializer).EnumerateSolutions();

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

        return new SatSolverInternal<
            ConstraintFactory,
            CandidateHeap,
            VariableTrail<CandidateHeap>,
            VariablePropagator<VariableTrail<CandidateHeap>, ActivityManager<CandidateHeap>, PropagationRateTracker>,
            ConflictHandler<
                ActivityManager<CandidateHeap>,
                VariableTrail<CandidateHeap>,
                PropagationRateTracker,
                LiteralBlockDistanceTracker,
                LearnedConstraintCreator<VariableTrail<CandidateHeap>, ActivityManager<CandidateHeap>>,
                RestartManager<VariableTrail<CandidateHeap>, PropagationRateTracker, LiteralBlockDistanceTracker, LearnedConstraintsReducer<ConstraintFactory>, LubySequence>,
                ConstraintMinimizer,
                ConstraintFactory>,
            ActivityManager<CandidateHeap>,
            PropagationRateTracker,
            LearnedConstraintsReducer<ConstraintFactory>,
            RestartManager<VariableTrail<CandidateHeap>, PropagationRateTracker, LiteralBlockDistanceTracker, LearnedConstraintsReducer<ConstraintFactory>, LubySequence>>
            (new SatSolverInitializer(problem, options ?? SatSolverOptions.Default, cancellationToken)).EnumerateSolutions();
    }
}
