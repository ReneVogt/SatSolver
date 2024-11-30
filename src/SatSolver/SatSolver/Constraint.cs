namespace Revo.SatSolver;

sealed class Constraint
{
    /// <summary>
    /// This set contains all positive litereals in this clause.
    /// </summary>
    public HashSet<int> Positives { get; } = [];
    /// <summary>
    /// This set contains all negative litereals in this clause.
    /// </summary>
    public HashSet<int> Negatives { get; } = [];

    /// <summary>
    /// Indicates at which step this clause was removed to 
    /// know when to put in use again during back tracking.
    /// </summary>
    public int RemovalIndex { get; set; }
}
