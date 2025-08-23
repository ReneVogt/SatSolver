namespace Revo.SatSolver.DataStructures;

sealed class UnitPropagationQueue : Queue<(ConstraintLiteral UnitLiteral, Constraint? Reason)>
{
}
