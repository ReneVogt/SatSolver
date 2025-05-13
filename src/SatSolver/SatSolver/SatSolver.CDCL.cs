using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver;

public sealed partial class SatSolver
{
    void PerformClauseLearning(Constraint conflictingConstraint)
    {
        var learnedConstraint = CreateLearnedConstraint(conflictingConstraint, out var uipLiteral);

        foreach (var l in learnedConstraint.Literals) IncreaseVariableActivity(l);
        _variableActivityIncrement /= _options.VariableActivityDecayFactor;

        _unitLiterals.Clear();
        _unitLiterals.Enqueue((uipLiteral, learnedConstraint));

        if (learnedConstraint.LiteralBlockDistance > _options.LiteralBlockDistanceMaximum)
        {
            JumpBack(learnedConstraint, uipLiteral);
            return;
        }

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

        if (_options.ClauseDeletion?.OriginalClauseCountFactor is { } f && _learnedConstraints.Count > f * _originalClauseCount)
            ReduceClauses();

        JumpBack(learnedConstraint, uipLiteral);
        CheckLiteralBlockDistanceBehaviour(learnedConstraint.LiteralBlockDistance);
    }

    readonly HashSet<int> _literalBlockDistanceCounter = [];
    Constraint CreateLearnedConstraint(Constraint conflictingConstraint, out int uipLiteral)
    {
        var literals = _literals;
        var learnedLiterals = new HashSet<int>();
        var conflicts = 0;

        foreach (var literal in conflictingConstraint.Literals)
        {
            learnedLiterals.Add(literal);
            if (literals[literal &-2].DecisionLevel == _decisionLevels.Count) conflicts++;
        }

        for (int trailIndex = _variableTrailSize-1; conflicts > 1; trailIndex--)
        {
            var trailedVariable = _variableTrail[trailIndex];
            var trailedPositiveLiteralIndex = trailedVariable << 1;
            var trailedPositiveLiteral = literals[trailedPositiveLiteralIndex];
            if (trailedPositiveLiteral.Reason is null) continue;

            var literalToResolve = trailedPositiveLiteralIndex;
            if (trailedPositiveLiteral.Sense == true) literalToResolve += 1;
            var negatedLiteralToResolve = literalToResolve ^ 1;
            if (!learnedLiterals.Remove(literalToResolve)) continue;

            IncreaseClauseActivity(trailedPositiveLiteral.Reason);            

            foreach (var reasonLiteral in trailedPositiveLiteral.Reason.Literals)
            {
                if (reasonLiteral == negatedLiteralToResolve) continue;
                if (learnedLiterals.Add(reasonLiteral) && literals[reasonLiteral&-2].DecisionLevel == _decisionLevels.Count)
                    conflicts++;
            }

            conflicts--;
        }

        uipLiteral = learnedLiterals.First(l => literals[l&-2].DecisionLevel == _decisionLevels.Count);

        MinimizeClause(learnedLiterals, uipLiteral);

        _literalBlockDistanceCounter.Clear();
        foreach (var level in learnedLiterals.Select(l => _literals[l&-2].DecisionLevel))
            _literalBlockDistanceCounter.Add(level);

        var learnedConstraint = new Constraint([.. learnedLiterals]) { LiteralBlockDistance = _literalBlockDistanceCounter.Count, Activity = _clauseActivityIncrement };
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
    void JumpBack(Constraint learnedConstraint, int uipLiteral)
    {
        var literals = _literals;
        var uipLevel = literals[uipLiteral & -2].DecisionLevel;
        var level = 0;
        foreach (var learnedLiteral in learnedConstraint.Literals)
        {
            var ll = literals[learnedLiteral & -2].DecisionLevel;
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
        var literals = _literals;
        var constraintsToRemove = _learnedConstraints.Where(constraint => constraint.Literals.Count > 2 && constraint.LiteralBlockDistance > _options.ClauseDeletion!.LiteralBlockDistanceToKeep).OrderBy(constraint => constraint.Activity).ToArray();
        var countToDelete = Math.Max(constraintsToRemove.Length, (int)(constraintsToRemove.Length * _options.ClauseDeletion.RatioToDelete));
        for (var i=0; i<countToDelete; i++)
        {
            var constraint = constraintsToRemove[i];
            _learnedConstraints.Remove(constraint);
            literals[constraint.Watched1].Watchers.Remove(constraint);
            literals[constraint.Watched2].Watchers.Remove(constraint);
        }
    }

    void IncreaseClauseActivity(Constraint constraint, double factor = 1)
    {
        if (!constraint.IsLearned) return;

        constraint.Activity += _clauseActivityIncrement * factor;
        if (constraint.Activity < _rescaleLimit) return;
        
        foreach (var learnedConstraint in _learnedConstraints)
            learnedConstraint.Activity /= _rescaleLimit;
        _clauseActivityIncrement /= _rescaleLimit;
    }
    void IncreaseVariableActivity(int literal)
    {
        var literals = _literals;
        var activity = literals[literal & -2].Activity += _variableActivityIncrement;
        if (activity < _rescaleLimit) return;        
        for (var i = 0; i < _literals.Length; i+=2)
            literals[i].Activity /= _rescaleLimit;
        _variableActivityIncrement /= _rescaleLimit;
        _candidateHeap.Rescale(_rescaleLimit);
    }

    void CheckLiteralBlockDistanceBehaviour(int lbd)
    {
        if (_literalBlockDistanceTracker is null) return;
        _literalBlockDistanceTracker.AddValue(lbd);

        if (_options.Restart is { LiteralBlockDistanceThreshold: { } restartThreshold } &&  _literalBlockDistanceTracker.CurrentRatio > restartThreshold)
                _restartRecommended = true;
        if (_options.ClauseDeletion is { LiteralBlockDistanceThreshold: { } clauseDeletionThreshold} &&  _literalBlockDistanceTracker.CurrentRatio > clauseDeletionThreshold)
            ReduceClauses();
    }
}
