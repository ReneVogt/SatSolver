﻿namespace Revo.SatSolver.DataStructures;
sealed class Constraint
{
    public ConstraintLiteral[] Literals { get; }
    public ConstraintLiteral Watched1 { get; set; }
    public ConstraintLiteral Watched2 { get; set; }

    public int LiteralBlockDistance { get; init; }
    public double Activity { get; set; }
    public bool IsTracked { get; set; }

    public Constraint(IEnumerable<ConstraintLiteral> literals)
    {
        Literals = [.. literals];
        Watched1 = Literals[0];
        Watched1.Watchers.Add(this);
        if (Literals.Length > 1)
        {
            Watched2 = Literals[1];
            Watched2.Watchers.Add(this);
        }
        else
            Watched2 = Watched1;
    }
}
