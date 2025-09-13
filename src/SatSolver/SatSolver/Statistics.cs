using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Revo.SatSolver;

[ExcludeFromCodeCoverage]
sealed class Statistics(ITrackPropagationRate? _propagationRateTracker, ITrackLiteralBlockDistance? _literalBlockDistanceTracker)
{
    int _binaryConstraints;
    int _omittedLearnedConstraints;
    int _activeLearnedConstraints;
    int _permanentLearnedConstraints;
    int _removedLearnedConstraints;

    long _learnedLiteralsCount;
    long _minimizedLiteralsCount;

    int _decisions;
    int _propagations;

    [Conditional("DEBUG")]
    public void LogPropagation(Variable variable)
    {
        if (variable.Reason is null)
        {
            _decisions++;
            Debug.WriteLine($"DECISION [{variable.DecisionLevel}] {variable.Index+1} {variable.Sense}");
        }
        else
        {
            _propagations++;
            Debug.WriteLine($"PROPAGATION [{variable.DecisionLevel}] {variable.Index+1} {variable.Sense}");
        }
    }
    [Conditional("DEBUG")]
    public static void LogBackJump(int currentLevel, int targetLevel) => Debug.WriteLine($"BACKJUMP [{currentLevel}] {targetLevel}");
    
    [Conditional("DEBUG")]
    public void AddConflict(Constraint conflictingConstraint, Constraint learnedConstraint, int unminimizedLength)
    {
        var uip = learnedConstraint.Literals.MaxBy(l => l.Variable.DecisionLevel)!;

        Debug.WriteLine($"CONFLICT [{uip.Variable.DecisionLevel}]");
        Debug.WriteLine($"Decisions: {_decisions}");
        Debug.WriteLine($"Propagations: {_propagations}");
        Debug.WriteLine($"Propagation rate: {_propagationRateTracker?.CurrentRatio} ({_propagationRateTracker?.Average})");
        Debug.WriteLine($"LBD rate: {_literalBlockDistanceTracker?.CurrentRatio} ({_literalBlockDistanceTracker?.Average})");
        Debug.WriteLine($"Permanently learned constraints: {_permanentLearnedConstraints}");
        Debug.WriteLine($"Active learned constraints: {_activeLearnedConstraints}");
        Debug.WriteLine($"Omitted learned constraints: {_omittedLearnedConstraints}");
        Debug.WriteLine($"Deleted learned constraints:  {_removedLearnedConstraints}");
        Debug.WriteLine($"Binary constraints: {_binaryConstraints}");
        if (_learnedLiteralsCount != 0)
            Debug.WriteLine($"Minimization rate: {(100 - 100*(double)_minimizedLiteralsCount/_learnedLiteralsCount):0.00}%");
        else
            Debug.WriteLine($"Minimization rate: {0:0.00}%");

        Debug.WriteLine($"Conflicting constraint: {conflictingConstraint}");
        Debug.WriteLine($"- Learned: {conflictingConstraint.IsTracked}");
        Debug.WriteLine($"- Additional: {conflictingConstraint.IsTracked}");
        Debug.WriteLine($"Learned constraint: {learnedConstraint}");
        Debug.WriteLine($"- UIP: {(uip.Orientation ? "" : "-")}{uip.Variable.Index+1}");
        Debug.WriteLine($"- LBD: {learnedConstraint.LiteralBlockDistance}");
        Debug.WriteLine($"- Tracked: {learnedConstraint.IsTracked}");
        Debug.WriteLine($"- Omitted: {learnedConstraint.IsOmitted}");

        _learnedLiteralsCount += unminimizedLength;
        _minimizedLiteralsCount += learnedConstraint.Literals.Length;

        if (learnedConstraint.Literals.Length == 2)
            _binaryConstraints++;

        if (learnedConstraint.IsOmitted)
            _omittedLearnedConstraints++;
        else
        {
            _activeLearnedConstraints++;
            if (!learnedConstraint.IsTracked)
                _permanentLearnedConstraints++;
        }

        _propagations = _decisions = 0;
    }

    [Conditional("DEBUG")]
    public static void AddConflict(Constraint conflictingConstraint) => Debug.WriteLine($"CONFLICT [{conflictingConstraint.Literals.Select(l => l.Variable.DecisionLevel).Max()}] {conflictingConstraint}");

    [Conditional("DEBUG")]
    public void LogConstraintDeletion(int previousCount, int deleted, int conflicts, int conflictInterval)
    {
        _removedLearnedConstraints += deleted;
        _activeLearnedConstraints -= deleted;
        Debug.WriteLine($"REDUCING LEARNED CONSTRAINTS (currently {previousCount}): conflicts {conflicts} / {conflictInterval}.");
    }
    [Conditional("DEBUG")]
    public static void LogRestart(long counter, long interval, double propagationRateRatio, double lbdRatio) => Debug.WriteLine($"RESTART (counter: {counter} / {interval}, propagation rate: {propagationRateRatio}, lbd: {lbdRatio}).");

    [Conditional("DEBUG")]
    public static void DeliveringSolution(Literal[] solution) => Debug.WriteLine($"SOLUTION [{string.Join(" ", solution.AsEnumerable())}].");

    [Conditional("DEBUG")]
    public static void NoMoreSolutions() => Debug.WriteLine("NO MORE SOLUTIONS.");
}
