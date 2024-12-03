namespace Revo.SatSolver;


/// <summary>
/// Represents a SATisfiability problem with literals and clauses.
/// </summary>
/// <param name="NumberOfLiterals">The number of literals required by the problem.</param>
/// <param name="Clauses">The clauses that need to be satisfied in this problem.</param>
public sealed record Problem(int NumberOfLiterals, Clause[] Clauses)
{
    /// <summary>
    /// The number of clauses in this problem.
    /// </summary>
    public int NumberOfClauses => Clauses?.Length ?? 0;
}

