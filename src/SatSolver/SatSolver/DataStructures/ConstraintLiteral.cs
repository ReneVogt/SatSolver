using System.Data;

namespace Revo.SatSolver.DataStructures;
sealed class ConstraintLiteral(Variable variable, bool orientation)
{
    public Variable Variable { get; } = variable;
    public bool Orientation { get; } = orientation;
    public bool? Sense { get; set; }
    public List<Constraint> Watchers { get; } = [];
}
