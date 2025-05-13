using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver;

public sealed partial class SatSolver
{
    bool PropagateVariable(int variable, bool sense, Constraint? reason) 
    {
        var literals = _literals;
        var positiveLiteralIndex = variable << 1;
        var negativeLiteralIndex = positiveLiteralIndex + 1;
        literals[positiveLiteralIndex].Sense = sense;
        literals[positiveLiteralIndex].Reason = reason;
        literals[positiveLiteralIndex].DecisionLevel = _decisionLevels.Count;
        literals[negativeLiteralIndex].Sense = !sense;
        _variableTrail[_variableTrailSize++] = variable;        

        var watchedLiteral = sense ? negativeLiteralIndex : positiveLiteralIndex;
        var watchers = literals[watchedLiteral].Watchers;
        for(var watcherIndex = 0; watcherIndex<watchers.Count; watcherIndex++)
        {            
            var constraint = watchers[watcherIndex];
            if (constraint.Watched1 == watchedLiteral)
            {
                constraint.Watched1 = constraint.Watched2;
                constraint.Watched2 = watchedLiteral;
            }

            var otherWatchedSense = literals[constraint.Watched1].Sense;
            if (otherWatchedSense == true) continue;

            var nextLiteral = -1;
            foreach (var next in constraint.Literals)
            {
                if (next == watchedLiteral || next == constraint.Watched1) continue;
                var nextSense = literals[next].Sense;
                if (nextSense != false) nextLiteral = next;
                if (nextSense == true) break;
            }

            if (nextLiteral < 0)
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
            literals[nextLiteral].Watchers.Add(constraint);
            watchers.RemoveAt(watcherIndex--);            
        }

        literals[positiveLiteralIndex].Polarity = sense;
        return true;
    }
    bool PropagateUnits()
    {
        var literals = _literals;
        while(_unitLiterals.Count > 0)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var (literal, reason) = _unitLiterals.Dequeue();
            if (literals[literal].Sense is not null) continue;
            if (!PropagateVariable(literal >> 1, (literal & 1) == 0, reason)) return false;
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
                IncreaseVariableActivity(literal);
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

    (int Variable, bool Sense) Backtrack()
    {
        _unitLiterals.Clear();

        var first = false;
        var variableTrailIndex = -1;
        while (_decisionLevels.Count > 0 && !first) (variableTrailIndex, first) = _decisionLevels.Pop();
        if (!first) return (-1, true);

        var variable = _variableTrail[variableTrailIndex];
        var sense = !_literals[variable << 1].Sense!.Value;

        ResetVariableTrail(variableTrailIndex);
        return (variable, sense);
    }
}
