namespace Revo.SatSolver.DPLL;

sealed record Constraint(int Index)
{
    /// <summary>
    /// The variables/literals in this constraint
    /// that are not negated.
    /// </summary>
    public List<Variable> Positives { get; } = [];

    /// <summary>
    /// The negated variables/literals in 
    /// this constraint.
    /// </summary>
    public List<Variable> Negatives { get; } = [];

    /// <summary>
    /// The number of not-negated literals still active 
    /// in this constraint.
    /// </summary>
    public int NumberOfActivePositives { get; set; }

    /// <summary>
    /// The number of negated literals still active 
    /// in this constraint.
    /// </summary>
    public int NumberOfActiveNegatives { get; set; }

    /// <summary>
    /// Indicates at which step this clause was removed to 
    /// know when to put in use again during back tracking.
    /// </summary>
    public int RemovalIndex { get; set; } = -1;
}
