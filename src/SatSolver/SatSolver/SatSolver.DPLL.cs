namespace Revo.SatSolver;

public sealed partial class SatSolver
{
    struct Variable
    {
        public bool? Sense { get; set; }
        public double Activity { get; set; }
        public bool Polarity { get; set; }
        
        public Constraint? Reason { get; set; }
        public int DecisionLevel { get; set; }

        List<Constraint>? _watchers;
        public List<Constraint> Watchers => _watchers ??= [];

        public override readonly string ToString() => $"Sense: {Sense?.ToString() ?? "null"} Activity: {Activity} Polarity: {Polarity}";
    }
    class Constraint(HashSet<int> literals) // not a record to use reference equality!
    {
        public HashSet<int> Literals => literals;
        public int Watched1 { get; set; } = -1;
        public int Watched2 { get; set; } = -1;

        public int LiteralBlockDistance { get; init; }
        public double Activity { get; set; }
        public bool IsLearned => LiteralBlockDistance > 0;
    }

    bool PropagateVariable(int variable, bool sense, Constraint? reason) 
    {
        var positiveLiteralIndex = variable << 1;
        var negativeLiteralIndex = positiveLiteralIndex + 1;
        _literals[positiveLiteralIndex].Sense = sense;
        _literals[positiveLiteralIndex].Reason = reason;
        _literals[positiveLiteralIndex].DecisionLevel = _decisionLevels.Count;
        _literals[negativeLiteralIndex].Sense = !sense;
        _variableTrail[_variableTrailSize++] = variable;

        var watchedLiteral = sense ? negativeLiteralIndex : positiveLiteralIndex;
        var watchers = _literals[watchedLiteral].Watchers;
        for(var watcherIndex = 0; watcherIndex<watchers.Count; watcherIndex++)
        {            
            var constraint = watchers[watcherIndex];
            if (constraint.Watched1 == watchedLiteral)
            {
                constraint.Watched1 = constraint.Watched2;
                constraint.Watched2 = watchedLiteral;
            }

            var otherWatchedSense = _literals[constraint.Watched1].Sense;
            if (otherWatchedSense == true) continue;

            var nextLiteral = -1;
            foreach (var next in constraint.Literals)
            {
                if (next == watchedLiteral || next == constraint.Watched1) continue;
                var nextSense = _literals[next].Sense;
                if (nextSense != false) nextLiteral = next;
                if (nextSense == true) break;
            }

            if (nextLiteral < 0)
            {
                if (otherWatchedSense is not null)
                    return HandleConflict(constraint, reason);
                _unitLiterals.Enqueue((constraint.Watched1, constraint));
                IncreaseClauseActivity(constraint, 0.5);
                continue;
            }
            
            constraint.Watched2 = nextLiteral;
            _literals[nextLiteral].Watchers.Add(constraint);
            watchers.RemoveAt(watcherIndex--);            
        }

        _literals[positiveLiteralIndex].Polarity = sense;
        return true;
    }
    bool PropagateUnits()
    {
        while(_unitLiterals.Count > 0)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var (literal, reason) = _unitLiterals.Dequeue();
            if (_literals[literal].Sense is not null) continue;
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
