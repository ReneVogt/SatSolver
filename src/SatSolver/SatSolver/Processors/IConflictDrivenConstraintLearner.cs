using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.Processors;
interface IConflictDrivenConstraintLearner
{
    (ConstraintLiteral uip, Constraint reason) PerformClauseLearning(Constraint conflictingConstraint);
}