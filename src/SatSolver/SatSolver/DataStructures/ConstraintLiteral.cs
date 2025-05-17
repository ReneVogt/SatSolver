using System.Diagnostics;

namespace Revo.SatSolver.DataStructures;

[DebuggerDisplay("{" + nameof(_hash) + "} {" + nameof(Sense) + "}")]
sealed class ConstraintLiteral(Variable variable, bool orientation)
{
    readonly int _hash = orientation ? (variable.Index + 1) : -(variable.Index+1);
    public Variable Variable { get; } = variable;
    public bool Orientation { get; } = orientation;
    public bool? Sense { get; set; }
    public List<Constraint> Watchers { get; } = [];

    public override int GetHashCode() => _hash;
}
