using Revo.SatSolver.DataStructures;
using System.Diagnostics;

namespace Revo.SatSolver;

static class Statistics
{
    [ThreadStatic] static ComponentStore? _store;

    [ThreadStatic] static int _omittedLearnedConstraints;
    [ThreadStatic] static int _permantentLearnedConstraints;
    [ThreadStatic] static int _trackedLearnedConstraints;
    [ThreadStatic] static int _totalLearnedConstraints;

    [Conditional("DEBUG")]
    public static void Initialize(ComponentStore store)
    {
        _store = store;
        _omittedLearnedConstraints = _permantentLearnedConstraints = _trackedLearnedConstraints = _totalLearnedConstraints = 0;
    }

    [Conditional("DEBUG")]
    public static void AddLearnedConstraint(Constraint constraint)
    {
        var uip = constraint.Literals.MaxBy(l => l.Variable.DecisionLevel)!;
        Debug.WriteLine($"Created learned constraint: {constraint}");
        Debug.WriteLine($"UIP: {(uip.Orientation ? "" : "-")}{uip.Variable.Index+1}");
        Debug.WriteLine($"LBD: {constraint.LiteralBlockDistance}");
        Debug.WriteLine($"Tracked: {constraint.IsTracked}");
        Debug.WriteLine($"Omitted: {constraint.IsOmitted}");
        if (constraint.IsOmitted)
            _omittedLearnedConstraints++;
        else if (constraint.IsTracked)
            _trackedLearnedConstraints++;
        else
            _permantentLearnedConstraints++;
        
        _totalLearnedConstraints++;
    }
    [Conditional("DEBUG")]
    public static void AddReducedLearnedConstraint(int count)
    {
        _totalLearnedConstraints -= count;
        _trackedLearnedConstraints -= count;
    }

    [Conditional("DEBUG")]
    public static void Dump()
    {
        Debug.WriteLine("STATE:");
        Debug.WriteLine($"Active learned constraints:    {_totalLearnedConstraints}");
        Debug.WriteLine($"Tracked learned constraints:   {_trackedLearnedConstraints}");
        Debug.WriteLine($"Omitted learned constraints:   {_omittedLearnedConstraints}");
        Debug.WriteLine($"Permanent learned constraints: {_permantentLearnedConstraints}");
        Debug.WriteLine($"Propagation rate:              {_store?.PropagationRateTracker.CurrentRatio}");
        Debug.WriteLine($"LBD rate:                      {_store?.LiteralBlockDistanceTracker.CurrentRatio}.");
    }
}
