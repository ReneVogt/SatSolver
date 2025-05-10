namespace Revo.SatSolver;

public sealed partial class SatSolver
{
    Constraint CreateLearnedConstraint(Constraint conflictingConstraint, out int uipLiteral)
    {
        var learnedLiterals = new HashSet<int>();
        var conflicts = 0;

        foreach (var literal in conflictingConstraint.Literals)
        {
            learnedLiterals.Add(literal);
            if (_literals[literal &-2].DecisionLevel == _decisionLevels.Count) conflicts++;
        }

        for (int trailIndex = _variableTrailSize-1; conflicts > 1; trailIndex--)
        {
            var trailedVariable = _variableTrail[trailIndex];
            var trailedPositiveLiteralIndex = trailedVariable << 1;
            var trailedPositiveLiteral = _literals[trailedPositiveLiteralIndex];
            if (trailedPositiveLiteral.Reason is null) continue;

            var literalToResolve = trailedPositiveLiteralIndex;
            if (trailedPositiveLiteral.Sense == true) literalToResolve += 1;
            var negatedLiteralToResolve = literalToResolve ^ 1;
            if (!learnedLiterals.Remove(literalToResolve)) continue;

            IncreaseClauseActivity(trailedPositiveLiteral.Reason);            

            foreach (var reasonLiteral in trailedPositiveLiteral.Reason.Literals)
            {
                if (reasonLiteral == negatedLiteralToResolve) continue;
                if (learnedLiterals.Add(reasonLiteral) && _literals[reasonLiteral&-2].DecisionLevel == _decisionLevels.Count)
                    conflicts++;
            }

            conflicts--;
        }

        uipLiteral = learnedLiterals.First(l => _literals[l&-2].DecisionLevel == _decisionLevels.Count);

        MinimizeClause(learnedLiterals, uipLiteral);

        var learnedConstraint = new Constraint([.. learnedLiterals]) { LiteralBlockDistance = learnedLiterals.Select(l => _literals[l&-2].DecisionLevel).ToHashSet().Count, Activity = _clauseActivityIncrement };
        _clauseActivityIncrement /= _options.ClauseActivityDecayFactor;
        return learnedConstraint;
    }
    void MinimizeClause(HashSet<int> literals, int uipLiteral)
    {
        literals.RemoveWhere(l => l != uipLiteral && IsRedundant(l, 0));
        bool IsRedundant(int literal, int depth)
        {
            if (depth > 9) return false;
            var reason = _literals[literal &-2].Reason;
            if (reason is null) return false;
            return reason.Literals.All(r =>
                (r ^ 1) == literal ||
                literals.Contains(r) ||
                IsRedundant(r, depth+1));
        }
    }
    void AddLearnedConstraintIfUseful(Constraint learnedConstraint, int uipLiteral)
    {
        if (learnedConstraint.LiteralBlockDistance > _options.LiteralBlockDistanceLimit) return;

        _learnedConstraints.Add(learnedConstraint);

        learnedConstraint.Watched1 = uipLiteral;
        _literals[learnedConstraint.Watched1].Watchers.Add(learnedConstraint);
        if (learnedConstraint.Literals.Count == 1)
            learnedConstraint.Watched2 = uipLiteral;
        else
        {
            learnedConstraint.Watched2 = learnedConstraint.Literals.First(l => l != uipLiteral);
            _literals[learnedConstraint.Watched2].Watchers.Add(learnedConstraint);
        }
    }
    void JumpBack(Constraint learnedConstraint, int uipLiteral)
    {
        var uipLevel = _literals[uipLiteral & -2].DecisionLevel;
        var level = 0;
        foreach (var learnedLiteral in learnedConstraint.Literals)
        {
            var ll = _literals[learnedLiteral & -2].DecisionLevel;
            if (ll > level && ll < uipLevel)
                level = ll;
        }

        var variableTrailIndex = 0;
        while (_decisionLevels.Count > level)
            (variableTrailIndex, _) = _decisionLevels.Pop();

        ResetVariableTrail(variableTrailIndex);
    }

    void ReduceClauses()
    {
        var constraintsToRemove = _learnedConstraints.Where(constraint => constraint.Literals.Count > 2 && constraint.LiteralBlockDistance > _options.LiteralBlockDistanceToKeep).OrderBy(constraint => constraint.Activity).ToArray();
        var countToDelete = Math.Max(constraintsToRemove.Length, (int)(constraintsToRemove.Length * _options.ClauseDeletionRatio));
        for (var i=0; i<countToDelete; i++)
        {
            var constraint = constraintsToRemove[i];
            _learnedConstraints.Remove(constraint);
            _literals[constraint.Watched1].Watchers.Remove(constraint);
            _literals[constraint.Watched2].Watchers.Remove(constraint);
        }
    }

    void IncreaseClauseActivity(Constraint constraint, double factor = 1)
    {
        constraint.Activity += _clauseActivityIncrement * factor;
        if (constraint.Activity < 1e100) return;
        
        foreach (var learnedConstraint in _learnedConstraints)
            learnedConstraint.Activity *= 1e-100;
        _clauseActivityIncrement *= 1e-100;
    }
    void IncreaseVariableActivity(int literal)
    {
        var activity = _literals[literal & -2].Activity += _variableActivityIncrement;
        if (activity < 1e100) return;
        for (var i = 0; i< _literals.Length; i+=2)
            _literals[i].Activity *= 1e-100;
        _variableActivityIncrement *= 1e-100;
    }

    void CheckLBDforRestart(int lbd)
    {
        _lbdQueue.Enqueue(lbd);
        if (_lbdQueue.Count > _options.LiteralBlockDistanceQueueSize)
        { 
            _lbdQueue.Dequeue();
            _globalLiteralBlockDistanceMean = _options.LiteralBlockDistanceDecay * _globalLiteralBlockDistanceMean + (1 - _options.LiteralBlockDistanceDecay) * lbd;
            var average = _lbdQueue.Average();
            if (average > _globalLiteralBlockDistanceMean * _options.RestartLiteralBlockDistanceThreshold)
                _restartRecommended = true;

            return;
        }
        if (_lbdQueue.Count < _options.LiteralBlockDistanceQueueSize) return;
        _globalLiteralBlockDistanceMean = _lbdQueue.Average();
    }
}
