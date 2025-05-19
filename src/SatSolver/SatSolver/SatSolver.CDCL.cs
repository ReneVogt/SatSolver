using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver;

public sealed partial class SatSolver
{
    void PerformClauseLearning(Constraint conflictingConstraint)
    {
        var learnedConstraint = CreateLearnedConstraint(conflictingConstraint, out var uipLiteral, out var jumpBackLevel);
        _activityManager.IncreaseVariableActivity(learnedConstraint);

        _unitLiterals.Clear();
        _unitLiterals.Enqueue((uipLiteral, learnedConstraint));

        if (learnedConstraint.LiteralBlockDistance > _options.MaximumLiteralBlockDistance)
        {
            _trail.JumpBack(jumpBackLevel);
            return;
        }

        if (_options.ClauseDeletion is { LiteralBlockDistanceToKeep: var lbdLimit } && learnedConstraint.LiteralBlockDistance > lbdLimit)
        {
            _learnedConstraints.Add(learnedConstraint);
            learnedConstraint.IsTracked = true;
        }

        learnedConstraint.Watched1 = uipLiteral;
        uipLiteral.Watchers.Add(learnedConstraint);
        if (learnedConstraint.Literals.Length == 1)
            learnedConstraint.Watched2 = uipLiteral;
        else
        {
            learnedConstraint.Watched2 = learnedConstraint.Literals.First(l => l != uipLiteral);
            learnedConstraint.Watched2.Watchers.Add(learnedConstraint);
        }

        if (_options.ClauseDeletion?.OriginalClauseCountFactor is { } f && _learnedConstraints.Count > f * _originalClauseCount)
            ReduceClauses();

        _trail.JumpBack(jumpBackLevel);
        CheckLiteralBlockDistanceBehaviour(learnedConstraint.LiteralBlockDistance);
    }

    readonly HashSet<int> _literalBlockDistanceCounter = [];
    readonly HashSet<ConstraintLiteral> _learnedLiterals = [];
    Constraint CreateLearnedConstraint(Constraint conflictingConstraint, out ConstraintLiteral uipLiteral, out int jumpBackLevel)
    {
        var variables = _variables;
        var conflicts = 0;

        var learnedLiterals = _learnedLiterals;
        learnedLiterals.Clear();

        foreach (var literal in conflictingConstraint.Literals)
        {
            learnedLiterals.Add(literal);
            if (literal.Variable.DecisionLevel == _trail.DecisionLevel) conflicts++;
        }

        for (int trailIndex = _trail.Count-1; conflicts > 1; trailIndex--)
        {
            var trailedVariable = _trail[trailIndex];
            var reason = trailedVariable.Reason;
            if (reason is null) continue;

            var (literalToResolve, negatedLiteralToResolve) = trailedVariable.Sense == true
                ? (trailedVariable.NegativeLiteral, trailedVariable.PositiveLiteral)
                : (trailedVariable.PositiveLiteral, trailedVariable.NegativeLiteral);

            if (!learnedLiterals.Remove(literalToResolve)) continue;

            _activityManager.IncreaseConstraintActivity(reason);

            foreach (var reasonLiteral in reason.Literals)
            {
                if (reasonLiteral == negatedLiteralToResolve) continue;
                if (learnedLiterals.Add(reasonLiteral) && reasonLiteral.Variable.DecisionLevel == _trail.DecisionLevel)
                    conflicts++;
            }

            conflicts--;
        }

        uipLiteral = learnedLiterals.First(l => l.Variable.DecisionLevel == _trail.DecisionLevel);

        MinimizeClause(learnedLiterals, uipLiteral);

        _literalBlockDistanceCounter.Clear();
        jumpBackLevel = 0;
        foreach (var level in learnedLiterals.Select(l => l.Variable.DecisionLevel))
        {
            _literalBlockDistanceCounter.Add(level);
            if (level < _trail.DecisionLevel && level > jumpBackLevel)
                jumpBackLevel = level;
        }

        var learnedConstraint = new Constraint([.. learnedLiterals]) 
        {
            Activity = _activityManager.ConstraintActivityIncrement,
            LiteralBlockDistance = _literalBlockDistanceCounter.Count
        };
        _activityManager.DecayConstraintActivity();
        return learnedConstraint;
    }
    void MinimizeClause(HashSet<ConstraintLiteral> literals, ConstraintLiteral uipLiteral)
    {
        var limit = _options.MaximumClauseMinimizationDepth;
        literals.RemoveWhere(l => l != uipLiteral && IsRedundant(l, 0));
        bool IsRedundant(ConstraintLiteral literal, int depth)
        {
            if (depth > limit) return false;
            var reason = literal.Variable.Reason;
            if (reason is null) return false;
            var ignoreLiteral = literal.Orientation ? literal.Variable.NegativeLiteral : literal.Variable.PositiveLiteral;
            return reason.Literals.All(r =>            
                ignoreLiteral == literal ||
                literals.Contains(r) ||
                IsRedundant(r, depth+1));
        }
    }

    void ReduceClauses()
    {
        var learnedConstraints = _learnedConstraints;
        learnedConstraints.Sort((left, right) => -left.Activity.CompareTo(right.Activity));
        var start = (int)(_learnedConstraints.Count * _options.ClauseDeletion!.RatioToDelete);
        for (var i=start; i<learnedConstraints.Count; i++)
        {
            var constraint = learnedConstraints[i];
            constraint.IsTracked = false;
            constraint.Watched1.Watchers.Remove(constraint);
            constraint.Watched2.Watchers.Remove(constraint);
        }
        learnedConstraints.RemoveRange(start, learnedConstraints.Count-start);
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
