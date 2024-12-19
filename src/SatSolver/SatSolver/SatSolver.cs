using Revo.SatSolver.DPLL;
using Revo.SatSolver.Properties;
using System.Diagnostics.CodeAnalysis;

namespace Revo.SatSolver;

/// <summary>
/// Finds variable configurations that satisfy all
/// clauses in a SATisfiability problem.
/// </summary>
public sealed class SatSolver
{
    readonly CancellationToken _cancellationToken;
    readonly DpllProcessor _state;

    SatSolver(Problem problem, SatSolverOptions options, CancellationToken cancellationToken)
    { 
        _ = problem ?? throw new ArgumentNullException(nameof(problem));
        _cancellationToken = cancellationToken;

        if (problem.NumberOfLiterals < 1)
            throw new ArgumentException(Resources.SatSolverArgumentException_NumberOfLiterals, nameof(problem));
        if (problem.Clauses.SelectMany(clause => clause.Literals.Select(literal => Math.Abs(literal.Id))).Any(id => id < 1 || id > problem.NumberOfLiterals))
            throw new ArgumentException(Resources.SatSolverArgumentException_InvalidLiterals, nameof(problem));

        _state = new DpllProcessor(problem, _cancellationToken)
        {
            UnitPropagation = options.UnitPropagation,
            PureLiteralElimination = options.PureLiteralElimination,
            RemoveSupersets = options.RemoveSupersets
        };
    }

    IEnumerable<Literal[]> Solve()
    {
        bool done;
        do
        {
            done = !_state.FindNextSolution(out var solution);
            if (solution is not null)
                yield return solution;

        } while (!done);
    }

    bool IsSatisfiable([NotNullWhen(true)] out Literal[]? solution) => _state.FindNextSolution(out solution);

    /// <summary>
    /// Finds all variable configurations that satisfy the SATisfiability <paramref name="problem"/>.
    /// The search is performed while iterating the result sequence.
    /// </summary>
    /// <param name="problem">The <see cref="Problem"/> to satisfy.</param>
    /// <param name="options">Options to configure algorithm elements of the <see cref="SatSolver"/>. Note:
    /// Some options like <see cref="SatSolverOptions.PureLiteralElimination"/> may exclude some
    /// possible solutions.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A sequence of solutions. The sequence is empty if no solution was found. The solution search
    /// is performed deferred and executed when iterating through this sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> was <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The problem contains either invalid literal IDs or no literals at all.</exception>
    public static IEnumerable<Literal[]> Solve(Problem problem, SatSolverOptions? options = null, CancellationToken cancellationToken = default)
    {
        var solver = new SatSolver(problem, options ?? SatSolverOptions.AllSolutions, cancellationToken);
        return solver.Solve();
    }

    /// <summary>
    /// Decides if the given SATisfiability <paramref name="problem"/> can be satisfied.
    /// </summary>
    /// <param name="problem">The <see cref="Problem"/> to satisfy.</param>
    /// <param name="options">Options to configure algorithm elements of the <see cref="SatSolver"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if a variable configuration exists that satisfies the <paramref name="problem"/>, 
    /// <c>false</c> if no such configuration exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> was <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The problem contains either invalid literal IDs or no literals at all.</exception>
    public static bool IsSatisfiable(Problem problem, SatSolverOptions? options = null, CancellationToken cancellationToken = default) => IsSatisfiable(problem, out _, options, cancellationToken);

    /// <summary>
    /// Decides if the given SATisfiability <paramref name="problem"/> can be satisfied.
    /// </summary>
    /// <param name="problem">The <see cref="Problem"/> to satisfy.</param>
    /// <param name="solution">When <c>true</c> is returned, this contains the first encountered solution. Otherwise this is <c>null</c>.</param>
    /// <param name="options">Options to configure algorithm elements of the <see cref="SatSolver"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if a variable configuration exists that satisfies the <paramref name="problem"/>, 
    /// <c>false</c> if no such configuration exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> was <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The problem contains either invalid literal IDs or no literals at all.</exception>
    public static bool IsSatisfiable(Problem problem, [NotNullWhen(true)] out Literal[]? solution, SatSolverOptions? options = null, CancellationToken cancellationToken = default)
    {
        var solver = new SatSolver(problem, options ?? SatSolverOptions.Satisfiability, cancellationToken);
        return solver.IsSatisfiable(out solution);
    }
}
