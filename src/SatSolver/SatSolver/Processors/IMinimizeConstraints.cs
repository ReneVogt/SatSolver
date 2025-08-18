using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.Processors;

interface IMinimizeConstraints 
{
    void MinimizeConstraint(HashSet<ConstraintLiteral> literals, int decisionLevel);
}
