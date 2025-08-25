using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.Processors;

interface ICreateLearnedConstraints 
{
    void CreateLearnedConstraint(Constraint conflictingConstraint, StampArray learnedLiterals);
}
