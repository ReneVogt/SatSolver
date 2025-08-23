using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.Processors;
interface IPropagateVariables
{
    Constraint? PropagateVariable(Variable variable, bool sense, Constraint? reason);
}