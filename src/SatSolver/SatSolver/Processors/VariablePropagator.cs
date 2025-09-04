using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Revo.SatSolver.Processors;

sealed class VariablePropagator<
    TVariableTrail, 
    TActivityManager, 
    TPropagationRateTracker>(IVariableTrail trail, UnitPropagationQueue _unitPropagationQueue, IManageActivities activityManager, ITrackPropagationRate propagationRateTracker) : IPropagateVariables
    where TVariableTrail : IVariableTrail
    where TActivityManager : IManageActivities
    where TPropagationRateTracker : ITrackPropagationRate
{
    readonly TVariableTrail _trail = (TVariableTrail)trail;
    readonly TActivityManager _activityManager = (TActivityManager)activityManager;
    readonly TPropagationRateTracker _propagationRateTracker = (TPropagationRateTracker)propagationRateTracker;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Constraint? PropagateVariable(Variable variable, bool sense, Constraint? reason)
    {
        variable.Sense = sense;
        variable.Reason = reason;
        _trail.Add(variable);

        var watchedLiteral = sense ? variable.NegativeLiteral : variable.PositiveLiteral;

        // binary fast path
        var binaries = watchedLiteral.Binaries;
        for(var i=0; i<binaries.Count; i++)
        {
            var binary = binaries[i];
            var value = binary.Literal.Sense;
            if (value == false) return binary.Reason;
            if (value == true) continue;

            _unitPropagationQueue.Enqueue(binary);
            _propagationRateTracker.AddPropagation();
        }

        var watchers = watchedLiteral.Watchers;
        for (var watcherIndex = 0; watcherIndex<watchers.Count; watcherIndex++)
        {
            var constraint = watchers[watcherIndex];
            if (constraint.Literals.Length == 1) return constraint;

            if (constraint.Watched1 == watchedLiteral)
            {
                constraint.Watched1 = constraint.Watched2;
                constraint.Watched2 = watchedLiteral;
            }

            var otherWatchedSense = constraint.Watched1.Sense;
            if (otherWatchedSense == true) continue;
            if (otherWatchedSense == false)
            {
                Debug.Assert(constraint.Literals.All(l => l.Sense == false));
                return constraint;
            }

            ConstraintLiteral? nextLiteral = null;
            for (var i=0; i<constraint.Literals.Length; i++)
            { 
                var next = constraint.Literals[i];
                if (next == watchedLiteral || next == constraint.Watched1) continue;
                var nextSense = next.Sense;
                if (nextSense != false) nextLiteral = next;
                if (nextSense == true) break;
            }

            if (nextLiteral is null)
            {
                _unitPropagationQueue.Enqueue((constraint.Watched1, constraint));
                _activityManager.IncreaseConstraintActivity(constraint, 0.5);
                _propagationRateTracker.AddPropagation();
                continue;
            }

            constraint.Watched2 = nextLiteral;
            nextLiteral.Watchers.Add(constraint);

            watchers.SwapRemove(watcherIndex);
            watcherIndex--;
        }

        variable.Polarity = sense;
        return null;
    }
}
