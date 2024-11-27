namespace Revo.SatSolver;

public sealed record Problem(int NumberOfLiterals, Clause[] Clauses)
{
    public int NumberOfClauses => Clauses?.Length ?? 0;
}

