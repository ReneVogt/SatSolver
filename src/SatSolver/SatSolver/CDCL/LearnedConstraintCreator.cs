using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
using System.Diagnostics;

namespace Revo.SatSolver.CDCL;

sealed class LearnedConstraintCreator(IVariableTrail _trail, IActivityManager _activityManager, IMinimizeConstraints _constraintMinimizer, Variable[] _variables) : ICreateLearnedConstraints
{
    readonly HashSet<int> _literalBlockDistanceCounter = [];
    readonly HashSet<ConstraintLiteral> _learnedLiterals = [];
    public Constraint CreateLearnedConstraint(Constraint conflictingConstraint, out ConstraintLiteral uipLiteral, out int jumpBackLevel)
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

        _constraintMinimizer.MinimizeConstraint(learnedLiterals, uipLiteral);

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
            LiteralBlockDistance = _literalBlockDistanceCounter.Count,
            IsLearned = true
        };
        _activityManager.DecayConstraintActivity();

        Debug.WriteLine($"Created learned constraint: {learnedConstraint}, uip: {(uipLiteral.Orientation ? "" : "-")}{uipLiteral.Variable.Index+1}.");
        return learnedConstraint;
    }
}
