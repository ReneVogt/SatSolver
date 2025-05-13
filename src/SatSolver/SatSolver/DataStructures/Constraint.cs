namespace Revo.SatSolver.DataStructures;
sealed class Constraint(HashSet<int> literals) // not a record to use reference equality!
{
    public HashSet<int> Literals => literals;
    public int Watched1 { get; set; } = -1;
    public int Watched2 { get; set; } = -1;

    public int LiteralBlockDistance { get; init; }
    public double Activity { get; set; }
    public bool IsLearned => LiteralBlockDistance > 0;
}
