using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;

namespace SatSolverTests.Stubs;
sealed class TestActivityManager : IActivityManager
{
    public int DecayCount { get; private set; }
    public List<(Constraint Constraint, double Factor)> IncreasedConstraints { get; } = [];
    public double ConstraintActivityIncrement { get; set; }
    public double VariableActivityIncrement { get; set; }

    public void DecayConstraintActivity() => DecayCount++;
    public void IncreaseConstraintActivity(Constraint constraint, double factor = 1)
    {
        IncreasedConstraints.Add((constraint, factor));
    }
    public void IncreaseVariableActivity(Constraint constraint) => throw new NotImplementedException();
}
