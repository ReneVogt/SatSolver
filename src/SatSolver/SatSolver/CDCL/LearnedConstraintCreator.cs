using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
using System.Diagnostics;

namespace Revo.SatSolver.CDCL;

sealed class LearnedConstraintCreator(IVariableTrail _trail, IActivityManager _activityManager, IMinimizeConstraints _constraintMinimizer, Variable[] _variables) : ICreateLearnedConstraints
{
    readonly StampArray _literalBlockDistanceCounter = new();
    readonly StampArray _learnedLiterals = new();
    readonly StampArray _seenVariables = new();
    readonly ConstraintLiteral[] _finalLiterals = new ConstraintLiteral[_variables.Length];

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
            learnedLiterals.Add(StampIndex(literal));
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

            if (!learnedLiterals.Remove(StampIndex(literalToResolve))) continue;

            var used = false;
            foreach (var reasonLiteral in reason.Literals)
            {
                if (seenVariables.Contains(reasonLiteral.Variable.Index)) continue;
                if (!learnedLiterals.Add(StampIndex(reasonLiteral))) continue;
                used = true;
                if (reasonLiteral.Variable.DecisionLevel == _trail.DecisionLevel)
                    conflicts++;
            }
            if (used) _activityManager.IncreaseConstraintActivity(reason);

            conflicts--;
        }

        var count = 0;
        foreach (var literal in learnedLiterals.EnumerateIndices().Select(ToLiteral))
            _finalLiterals[count++] = literal;

        //_constraintMinimizer.MinimizeConstraint(learnedLiterals, uipLiteral);

        _literalBlockDistanceCounter.Clear();
        jumpBackLevel = 0;
        ConstraintLiteral? uip = null;
        foreach (var literal in _finalLiterals.Take(count))
        {
            var level = literal.Variable.DecisionLevel;
            if (level == _trail.DecisionLevel)
            {
                uip = literal;
                continue;
            }
            _literalBlockDistanceCounter.Add(level);
            if (level < _trail.DecisionLevel && level > jumpBackLevel)
                jumpBackLevel = level;
        }

        Debug.Assert(uip is not null);
        uipLiteral = uip;

        var learnedConstraint = new Constraint([.. _finalLiterals.Take(count)], setWatchers: false)
        {
            Activity = _activityManager.ConstraintActivityIncrement,
            LiteralBlockDistance = _literalBlockDistanceCounter.Count,
            IsLearned = true
        };

        Debug.WriteLine($"Created learned constraint: {learnedConstraint}, uip: {(uipLiteral.Orientation ? "" : "-")}{uipLiteral.Variable.Index+1}.");
        return learnedConstraint;
    }

    static int StampIndex(ConstraintLiteral literal) => literal.Orientation ? literal.Variable.Index << 1 : ((literal.Variable.Index << 1) + 1);
    ConstraintLiteral ToLiteral(int index) => ((index & 1) == 1) ? _variables[index >> 1].NegativeLiteral : _variables[index >> 1].PositiveLiteral;
   
}
