using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;
using System.Diagnostics;

namespace Revo.SatSolver;

static class Statistics
{
    [ThreadStatic] static ITrackPropagationRate? _propagationRateTracker;
    [ThreadStatic] static ITrackLiteralBlockDistance? _literalBlockDistanceTracker;

    [ThreadStatic] static int _omittedLearnedConstraints;
    [ThreadStatic] static int _permantentLearnedConstraints;
    [ThreadStatic] static int _trackedLearnedConstraints;
    [ThreadStatic] static int _totalLearnedConstraints;

    [ThreadStatic] static long _learnedLiteralsCount;
    [ThreadStatic] static long _minimizedLiteralsCount;

    [Conditional("DEBUG")]
    public static void Initialize(ITrackPropagationRate propagationRateTracker, ITrackLiteralBlockDistance literalBlockDistanceTracker)
    {
        _propagationRateTracker = propagationRateTracker;
        _literalBlockDistanceTracker = literalBlockDistanceTracker;
        _omittedLearnedConstraints = _permantentLearnedConstraints = _trackedLearnedConstraints = _totalLearnedConstraints = 0;
        _learnedLiteralsCount = _minimizedLiteralsCount = 0;
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
        Debug.WriteLine($"Propagation rate:              {_propagationRateTracker?.CurrentRatio}");
        Debug.WriteLine($"LBD rate:                      {_literalBlockDistanceTracker?.CurrentRatio}");
        if (_learnedLiteralsCount != 0)
            Debug.WriteLine($"Minimization rate:             {(100 - 100*(double)_minimizedLiteralsCount/_learnedLiteralsCount):0.00}%");
    }


    [Conditional("DEBUG")]
    public static void StartConstraintMinimization(int initialCount)
    {
        Debug.WriteLine($"Minimizing constraint from {initialCount}.");
        _learnedLiteralsCount += initialCount;
    }
    [Conditional("DEBUG")]
    public static void FinishConstraintMinimization(int finalCount)
    {
        Debug.WriteLine($"Minimized constraint to {finalCount}.");
        _minimizedLiteralsCount += finalCount;
    }
}
