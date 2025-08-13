using Revo.SatSolver.DataStructures;
using System.Diagnostics;

namespace Revo.SatSolver.DPLL;
sealed class DpllProcessor(IVariableTrail trail, Queue<(ConstraintLiteral, Constraint Reason)> unitLiterals, IActivityManager activityManager, CancellationToken cancellationToken)
{
    readonly CancellationToken _cancellationToken = cancellationToken;
    readonly IVariableTrail _trail = trail;
    readonly Queue<(ConstraintLiteral, Constraint Reason)> _unitLiterals = unitLiterals;
    readonly IActivityManager _activityManager = activityManager;

    public Constraint? PropagateVariable(Variable variable, bool sense, Constraint? reason, out int propagationCount)
    {
        propagationCount = 0;

        variable.Sense = sense;
        variable.Reason = reason;
        _trail.Add(variable);

        var watchedLiteral = sense ? variable.NegativeLiteral : variable.PositiveLiteral;
        var watchers = watchedLiteral.Watchers;
        for (var watcherIndex = 0; watcherIndex<watchers.Count; watcherIndex++)
        {
            var constraint = watchers[watcherIndex];
            if (constraint.Watched1 == watchedLiteral)
            {
                constraint.Watched1 = constraint.Watched2;
                constraint.Watched2 = watchedLiteral;
            }

            var otherWatchedSense = constraint.Watched1.Sense;
            if (otherWatchedSense == true) continue;
            if (otherWatchedSense == false) return constraint;

            ConstraintLiteral? nextLiteral = null;
            foreach (var next in constraint.Literals)
            {
                if (next == watchedLiteral || next == constraint.Watched1) continue;
                var nextSense = next.Sense;
                if (nextSense != false) nextLiteral = next;
                if (nextSense == true) break;
            }

            if (nextLiteral is null)
            {               
                _unitLiterals.Enqueue((constraint.Watched1, constraint));
                _activityManager.IncreaseConstraintActivity(constraint, 0.5);
                propagationCount++;
                continue;
            }

            constraint.Watched2 = nextLiteral;
            nextLiteral.Watchers.Add(constraint);
            watchers.RemoveAt(watcherIndex--);
        }

        variable.Polarity = sense;
        return null;
    }
    public Constraint? PropagateUnits(ref int propagationCount)
    {
        while (_unitLiterals.Count > 0)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var (literal, reason) = _unitLiterals.Dequeue();
            if (literal.Sense is not null) continue;
            Debug.WriteLine($"[{_trail.DecisionLevel}] Propagating {literal.Variable.Index+1} to {literal.Orientation}.");
            var conflictingConstraint = PropagateVariable(literal.Variable, literal.Orientation, reason, out var props);
            propagationCount += props;
            if (conflictingConstraint is not null) return conflictingConstraint;
        }

        return null;
    }
}
