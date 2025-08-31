namespace Revo.SatSolver.DataStructures;

sealed class Constraint(ConstraintLiteral[] literals, ConstraintLiteral watched1, ConstraintLiteral watched2)
{
    public ConstraintLiteral[] Literals { get; } = literals;
    public ConstraintLiteral Watched1 { get; set; } = watched1;
    public ConstraintLiteral Watched2 { get; set; } = watched2;

    public int LiteralBlockDistance { get; set; }
    public double Activity { get; set; }
    public bool IsTracked { get; set; }

    public bool IsLearned { get; set; }
    public bool IsOmitted { get; set; }

    public bool IsAdditional{ get; set; }

    public override string ToString() => string.Join(" ", Literals.Select(l => $"{(l.Orientation ? "" : "-")}{l.Variable.Index+1}"));
}
