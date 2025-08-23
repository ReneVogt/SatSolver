using System.Diagnostics;

namespace Revo.SatSolver;

static class Statistics
{
    [ThreadStatic] static ComponentStore? _store;

    [ThreadStatic] static int _omittedLearnedConstraints;
    [ThreadStatic] static int _permantentLearnedConstraints;
    [ThreadStatic] static int _trackedLearnedConstraints;
    [ThreadStatic] static int _deletedLearnedConstraints;
    [ThreadStatic] static int _totalLearnedConstraints;

    [Conditional("DEBUG")]
    public static void Initialize(ComponentStore store)
    {
        _store = store;
        _omittedLearnedConstraints = _permantentLearnedConstraints = _trackedLearnedConstraints = _deletedLearnedConstraints =  _totalLearnedConstraints = 0;
    }

    [Conditional("DEBUG")]
    public static void AddOmittedLearnedConstraint()
    {
        _omittedLearnedConstraints++;
        _totalLearnedConstraints++;
    }
    [Conditional("DEBUG")]
    public static void AddTrackedLearnedConstraint()
    {
        _trackedLearnedConstraints++;
        _totalLearnedConstraints++;
    }
    [Conditional("DEBUG")]
    public static void AddPermanentLearnedConstraint()
    {
        _permantentLearnedConstraints++;
        _totalLearnedConstraints++;
    }
    [Conditional("DEBUG")]
    public static void AddReducedLearnedConstraint(int count)
    {
        _deletedLearnedConstraints += count;
    }

    [Conditional("DEBUG")]
    public static void Dump()
    {
        Debug.WriteLine($"State: active learned constraints {_totalLearnedConstraints - _deletedLearnedConstraints}, omitted learned constraints {_omittedLearnedConstraints}, propagation rate: {_store?.PropagationRateTracker.CurrentRatio}, lbd: {_store?.LiteralBlockDistanceTracker.CurrentRatio}.");
    }
}
