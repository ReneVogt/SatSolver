using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.Processors;

interface ICreateLearnedConstraints 
{
    Constraint CreateLearnedConstraint(Constraint conflictingConstraint, out ConstraintLiteral uipLiteral, out int jumpBackLevel);
}
