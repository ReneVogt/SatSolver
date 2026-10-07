using System.Diagnostics.CodeAnalysis;

namespace Revo.SatSolver;

/// <summary>
/// Entry points to create <see cref="ISatSolver"/> instances.
/// </summary>
[ExcludeFromCodeCoverage]
public static class SatSolverFactory
{
    /// <summary>
    /// Enumerates all complete satisfying assignments by adding already found solutions
    /// as inverted constraints. Each assignment includes every declared variable.
    /// Assignments differing only in unused variables are distinct models.
    /// </summary>
    /// <param name="solver">The <see cref="ISatSolver"/> to enumerate.</param>
    /// <param name="cancellationToken">A token to cancel the solver.</param>
    /// <returns>A sequence of distinct complete models, or an empty sequence if unsatisfiable.
    /// A problem with no variables and no clauses has one model: an empty array.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="solver"/> was <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">The solver was canceled.</exception>
    public static IEnumerable<Literal[]> EnumerateSolutions(this ISatSolver solver, CancellationToken cancellationToken = default)
    {
        _ = solver ?? throw new ArgumentNullException(nameof(solver));
        for(; ; )
        { 
            var solution = solver.FindSolution(cancellationToken);
            if (solution is null) yield break;
            yield return solution;
            if (solution.Length == 0) yield break;
            solver.AddClause(new Clause(solution.Select(l => new Literal(l.Id, !l.Sense))));
        }
    }

    /// <summary>
    /// Creates an instance of a <see cref="ISatSolver"/> that can try to solve the SATisfiability <paramref name="problem"/>.    
    /// </summary>
    /// <param name="problem">The <see cref="Problem"/> to satisfy.</param>
    /// <param name="options">The options for the solver.</param>
    /// <returns>An <see cref="ISatSolver"/> instance initialized with the given <paramref name="problem"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> was <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The problem contains either invalid literal IDs or empty clauses.</exception>
    public static ISatSolver Create(Problem problem, SatSolverOptions? options = null)
    {
        _ = problem ?? throw new ArgumentNullException(paramName: nameof(problem));
        if (problem.Clauses.Any(c => c.Literals.Length == 0))
            throw new ArgumentException(paramName: nameof(problem), message: "The problem contains empty clauses. This is not supported.");

        options ??= new();

        options.Validate();

        var store = new ComponentStore(options, problem);
        return new SatSolver(store);
    }
    /// <summary>
    /// Enumerates all complete satisfying assignments of the SATisfiability <paramref name="problem"/>.
    /// Each model assigns every ID in 1..NumberOfLiterals exactly once, including unused
    /// variables. Assignments differing only in unused variables are distinct models.
    /// </summary>
    /// <param name="problem">The <see cref="Problem"/> to satisfy.</param>
    /// <param name="options">The options for the solver.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A sequence of distinct complete models, or an empty sequence if unsatisfiable.
    /// A problem with no variables and no clauses has one model: an empty array.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> was <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The problem contains either invalid literal IDs or empty clauses.</exception>
    public static IEnumerable<Literal[]> EnumerateSolutions(this Problem problem, SatSolverOptions? options = null, CancellationToken cancellationToken = default) =>
        Create(problem, options).EnumerateSolutions(cancellationToken);

    // These are the entry points for unit tests. We can provide an alternative store
    // with mocks for all the required algorithm parts.
    internal static ISatSolver Create(ComponentStoreBase store) => new SatSolver(store);
    internal static IEnumerable<Literal[]> EnumerateSolutions(ComponentStoreBase store) => Create(store).EnumerateSolutions();
}
