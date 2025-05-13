using System.Data;

namespace Revo.SatSolver.DataStructures;
struct ConstraintLiteral
{
    public bool? Sense { get; set; }
    public double Activity { get; set; }
    public bool Polarity { get; set; }

    public Constraint? Reason { get; set; }
    public int DecisionLevel { get; set; }

    List<Constraint>? _watchers;
    public List<Constraint> Watchers => _watchers ??= [];

    public override readonly string ToString() => $"Sense: {Sense?.ToString() ?? "null"} Activity: {Activity} Polarity: {Polarity}";
}
