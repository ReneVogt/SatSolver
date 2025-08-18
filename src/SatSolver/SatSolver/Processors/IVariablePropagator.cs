using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.Processors;
interface IVariablePropagator
{
    Constraint? PropagateUnits(ref int propagationCount);
    Constraint? PropagateVariable(Variable variable, bool sense, Constraint? reason, out int propagationCount);
}