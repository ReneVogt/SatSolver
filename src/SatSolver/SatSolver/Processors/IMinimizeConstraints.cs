using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.Processors;

interface IMinimizeConstraints 
{
    void MinimizeConstraint(StampArray constraint, int decisionLevel, ConstraintLiteral[] knownLiterals);
}
