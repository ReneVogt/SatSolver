using System.Diagnostics;

namespace Revo.SatSolver.DataStructures;

[DebuggerDisplay("{" + nameof(StampIndex) + "} {" + nameof(Sense) + "}")]
sealed class ConstraintLiteral(Variable variable, bool orientation)
{
    public Variable Variable { get; } = variable;
    public bool Orientation { get; } = orientation;
    public bool? Sense { get; set; }
    public List<Constraint> Watchers { get; } = [];

    public int StampIndex { get; } = orientation ? variable.Index << 1 : ((variable.Index << 1) + 1);

    public override int GetHashCode() => StampIndex;
}
