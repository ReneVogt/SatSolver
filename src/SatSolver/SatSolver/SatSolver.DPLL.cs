using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver;

public sealed partial class SatSolver
{
    bool PropagateVariable(Variable variable, bool sense, Constraint? reason)
    {
        variable.Sense = sense;
        variable.Reason = reason;
        variable.DecisionLevel = _decisionLevels.Count;
        _variableTrail[_variableTrailSize++] = variable;        

        var watchedLiteral = sense ? variable.NegativeLiteral : variable.PositiveLiteral;
        var watchers = watchedLiteral.Watchers;
        for(var watcherIndex = 0; watcherIndex<watchers.Count; watcherIndex++)
        {            
            var constraint = watchers[watcherIndex];
            if (constraint.Watched1 == watchedLiteral)
            {
                constraint.Watched1 = constraint.Watched2;
                constraint.Watched2 = watchedLiteral;
            }

            var otherWatchedSense = constraint.Watched1.Sense;
            if (otherWatchedSense == true) continue;

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
                if (otherWatchedSense is not null)
                {
                    TrackPropagationRate(conflict: true);
                    return HandleConflict(constraint, reason);
                }
                _unitLiterals.Enqueue((constraint.Watched1, constraint));
                IncreaseClauseActivity(constraint, 0.5);
                TrackPropagationRate(conflict: false);
                continue;
            }
            
            constraint.Watched2 = nextLiteral;
            nextLiteral.Watchers.Add(constraint);
            watchers.RemoveAt(watcherIndex--);         
        }

        variable.Polarity = sense;
        return true;
    }
    bool PropagateUnits()
    {
        while(_unitLiterals.Count > 0)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var (literal, reason) = _unitLiterals.Dequeue();
            if (literal.Sense is not null) continue;
            if (!PropagateVariable(literal.Variable, literal.Orientation, reason)) return false;
        }

        return true;
    }

    bool HandleConflict(Constraint conflictingConstraint, Constraint? reason)
    {
        if (_nextRestartThreshold > 0)
        {
            _restartCounter++;
            _restartRecommended = _restartCounter > _nextRestartThreshold;
        }

        if (_options.OnlyPoorMansVSIDS)
        {
            foreach (var literal in conflictingConstraint.Literals)
                IncreaseVariableActivity(literal.Variable);
            _variableActivityIncrement /= _options.VariableActivityDecayFactor;
            return false;
        }

        IncreaseClauseActivity(conflictingConstraint);

        if (reason is null || _decisionLevels.Count == 0) return false;
        PerformClauseLearning(conflictingConstraint);
        return true;
    }

    void TrackPropagationRate(bool conflict)
    {
        if (_propagationRateTracker is null) return;
        if (conflict)
            _propagationRateTracker.AddConflict();
        else
            _propagationRateTracker.AddPropagation();

        if (_options.Restart is { PropagationRateThreshold: { } restartThreshold } &&  _propagationRateTracker.CurrentRatio < restartThreshold)
            _restartRecommended = true;
        if (_options.ClauseDeletion is { PropagationRateThreshold: { } clauseDeletionThreshold } && _propagationRateTracker.CurrentRatio < clauseDeletionThreshold)
            ReduceClauses();
    }

    (Variable? Variable, bool Sense) Backtrack()
    {
        _unitLiterals.Clear();

        var first = false;
        var variableTrailIndex = -1;
        while (_decisionLevels.Count > 0 && !first) (variableTrailIndex, first) = _decisionLevels.Pop();
        if (!first) return (null, true);

        var variable = _variableTrail[variableTrailIndex];
        var sense = !variable.Sense!.Value;

        ResetVariableTrail(variableTrailIndex);
        return (variable, sense);
    }
}
