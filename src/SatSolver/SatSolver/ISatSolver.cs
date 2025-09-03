namespace Revo.SatSolver;

/// <summary>
/// Represents a satisfiability solver.
/// </summary>
public interface ISatSolver
{
    /// <summary>
    /// Finds a solution for the current state of the solver.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the process.</param>
    /// <exception cref="OperationCanceledException">The solver was cancelled.</exception>
    Literal[]? FindSolution(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new clause to the current solver state.
    /// The solver's state will be reset as far as necessary
    /// to incorporate the clause correctly.
    /// </summary>
    /// <exception cref="OperationCanceledException">The solver was already cancelled.</exception>
    void AddClause(Clause clause);

    /// <summary>
    /// Resets the solver state by unassigning all variables to
    /// restart search.
    /// </summary>
    /// <param name="removeAdditionalClauses"><c>true</c> if the state
    /// should be reset completely to the initial state and forget all
    /// additional clauses. This also removes any learned clauses, 
    /// because they may be learned from one or more of the additional 
    /// clauses.</param>
    /// <exception cref="OperationCanceledException">The solver was already cancelled.</exception>
    void Reset(bool removeAdditionalClauses = false);
}
