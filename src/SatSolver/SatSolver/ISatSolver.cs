namespace Revo.SatSolver;

/// <summary>
/// Represents a satisfiability solver.
/// </summary>
public interface ISatSolver
{
    /// <summary>
    /// Finds a solution for the current state of the solver.
    /// Once the current clauses are known to be unsatisfiable, subsequent calls
    /// return <c>null</c> without searching again, unless <see cref="Reset"/>
    /// is called with <c>removeAdditionalClauses: true</c>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the process.</param>
    /// <returns>A complete satisfying assignment with exactly one entry for every ID
    /// in 1..NumberOfLiterals of the original problem, including variables absent from
    /// all clauses, or <c>null</c> if the current clauses are unsatisfiable.</returns>
    /// <remarks>Cancellation is checked even when the clauses are already known to be unsatisfiable.</remarks>
    /// <exception cref="OperationCanceledException">The solver was cancelled.</exception>
    Literal[]? FindSolution(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new clause to the current solver state.
    /// The solver's state will be reset as far as necessary
    /// to incorporate the clause correctly.
    /// Adding a clause preserves a known unsatisfiable state.
    /// </summary>
    /// <exception cref="ArgumentNullException">The clause is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The clause is empty or contains invalid literal IDs.</exception>
    void AddClause(Clause clause);

    /// <summary>
    /// Resets the solver state by unassigning all variables to
    /// restart search. If clauses are retained, a known unsatisfiable state
    /// is also retained.
    /// </summary>
    /// <param name="removeAdditionalClauses"><c>true</c> if the state
    /// should be reset completely to the initial state and forget all
    /// additional clauses. This also removes any learned clauses,
    /// because they may be learned from one or more of the additional
    /// clauses, and clears the known unsatisfiable state so that the original
    /// problem is evaluated again. <c>false</c> retains all clauses and any
    /// known unsatisfiable state.</param>
    void Reset(bool removeAdditionalClauses = false);
}
