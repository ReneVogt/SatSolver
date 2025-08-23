using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.Processors;
interface IHandleConflicts
{
    void HandleConflict(Constraint conflictingConstraint);
}