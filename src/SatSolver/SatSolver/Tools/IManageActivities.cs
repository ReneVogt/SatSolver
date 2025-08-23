using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.Tools;
interface IManageActivities
{
    double ConstraintActivityIncrement { get; }
    double VariableActivityIncrement { get; }

    void DecayConstraintActivity();
    void IncreaseConstraintActivity(Constraint constraint, double factor = 1);
    void IncreaseVariableActivity(Constraint constraint);
}