using Revo.SatSolver.CDCL;
using Revo.SatSolver.DataStructures;

namespace SatSolverTests.Stubs;

sealed class TestConstraintMinimizer : IMinimizeConstraints
{
    public List<(HashSet<ConstraintLiteral> Constraint, ConstraintLiteral Uip)> Calls { get; } = [];
    public void MinimizeConstraint(HashSet<ConstraintLiteral> literals, ConstraintLiteral uipLiteral) => Calls.Add((literals, uipLiteral));
}