using System.Diagnostics;

namespace Revo.SatSolver.DataStructures;

[DebuggerDisplay("{" + nameof(Index) + "} {" + nameof(Sense) + "} {" + nameof(DecisionLevel) + "} {" + nameof(Polarity) + "} {" + nameof(Activity) + "}")]
sealed class Variable
{
    public int Index { get; }
    public ConstraintLiteral PositiveLiteral { get; }
    public ConstraintLiteral NegativeLiteral { get; }
    public bool? Sense 
    {
        get =>  PositiveLiteral.Sense; 
        set
        {
            PositiveLiteral.Sense = value;
            NegativeLiteral.Sense = value is null ? null : !value.Value;
        }
    }
    public double Activity { get; set; }
    public bool Polarity { get; set; }

    public int DecisionLevel { get; set; }
    public Constraint? Reason { get; set; }

    public Variable(int index)
    {
        Index = index;
        PositiveLiteral = new(this, true);
        NegativeLiteral = new(this, false);
    }

    public override int GetHashCode() => Index;
}
