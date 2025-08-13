using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.CDCL;

interface ICreateLearnedConstraints 
{
    Constraint CreateLearnedConstraint(Constraint conflictingConstraint, out ConstraintLiteral uipLiteral, out int jumpBackLevel);
}
