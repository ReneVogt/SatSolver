namespace Revo.SatSolver;

sealed record Variable(int Id)
{
    public bool Sense { get; set; }

    /// <summary>
    /// This set contains the indices of the clauses where
    /// this variable is contained as positive literal.
    /// </summary>
    public HashSet<int> Positives { get; } = [];
    /// <summary>
    /// This set contains the indices of the clauses where
    /// this variable is contained as negative literal.
    /// </summary>
    public HashSet<int> Negatives { get; } = [];
}
