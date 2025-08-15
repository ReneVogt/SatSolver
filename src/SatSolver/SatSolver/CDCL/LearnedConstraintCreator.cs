using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
using Revo.SatSolver.Helpers;
using System.Diagnostics;

namespace Revo.SatSolver.CDCL;

sealed class LearnedConstraintCreator(IVariableTrail _trail, IActivityManager _activityManager, IMinimizeConstraints _constraintMinimizer, Variable[] _variables) : ICreateLearnedConstraints
{
    readonly HashSet<int> _literalBlockDistanceCounter = [];
    readonly HashSet<ConstraintLiteral> _learnedLiterals = [];
    readonly HashSet<int> _seenVariables = [];

    public Constraint CreateLearnedConstraint(Constraint conflictingConstraint, out ConstraintLiteral uipLiteral, out int jumpBackLevel)
    {
        var variables = _variables;
        var conflicts = 0;

        Debug.Assert(conflictingConstraint.Literals.All(l => l.Sense == false));

        var learnedLiterals = _learnedLiterals;
        var seenVariables = _seenVariables;
        learnedLiterals.Clear();
        seenVariables.Clear();

        foreach (var literal in conflictingConstraint.Literals)
        {
            seenVariables.Add(literal.Variable.Index);
            learnedLiterals.Add(literal);
            if (literal.Variable.DecisionLevel == _trail.DecisionLevel) conflicts++;
        }

        for (int trailIndex = _trail.Count-1; conflicts > 1; trailIndex--)
        {
            var trailedVariable = _trail[trailIndex];
            _seenVariables.Add(trailedVariable.Index);

            var reason = trailedVariable.Reason;
            if (reason is null) continue;

            var literalToResolve = trailedVariable.Sense == true
                ? trailedVariable.NegativeLiteral
                : trailedVariable.PositiveLiteral;

            if (!learnedLiterals.Remove(literalToResolve)) continue;

            _activityManager.IncreaseConstraintActivity(reason);

            foreach (var reasonLiteral in reason.Literals)
            {
                if (seenVariables.Contains(reasonLiteral.Variable.Index)) continue;
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

        var learnedConstraint = new Constraint([.. learnedLiterals], setWatchers: false)
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
