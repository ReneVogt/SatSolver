using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.CDCL;

interface IMinimizeConstraints 
{
    void MinimizeConstraint(HashSet<ConstraintLiteral> literals, ConstraintLiteral uipLiteral);
}
