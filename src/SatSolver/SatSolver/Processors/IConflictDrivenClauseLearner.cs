using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.Processors;
interface IConflictDrivenClauseLearner
{
    (ConstraintLiteral uip, Constraint reason) PerformClauseLearning(Constraint conflictingConstraint);
}