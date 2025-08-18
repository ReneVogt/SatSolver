using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;

namespace SatSolverTests.Stubs;

sealed class TestConstraintMinimizer : IMinimizeConstraints
{
    public List<(HashSet<ConstraintLiteral> Constraint, int DecisionLevel)> Calls { get; } = [];
    public void MinimizeConstraint(HashSet<ConstraintLiteral> literals, int decisionLevel) => Calls.Add((literals, decisionLevel));
}