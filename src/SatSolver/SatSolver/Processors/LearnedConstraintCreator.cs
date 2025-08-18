using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
using System.Diagnostics;

namespace Revo.SatSolver.Processors;

sealed class LearnedConstraintCreator(SatSolverState _state) : ICreateLearnedConstraints
{
    readonly IVariableTrail _trail = _state.VariableTrail;
    readonly IActivityManager _activityManager = _state.ActivityManager;
    readonly IMinimizeConstraints _constraintMinimizer = _state.ConstraintMinimizer;
    readonly Variable[] _variables = _state.Variables;
    readonly ConstraintLiteral[] _literals = _state.Literals;
    readonly StampArray _literalBlockDistanceCounter = new();
    readonly StampArray _learnedLiterals = new();
    readonly StampArray _seenVariables = new();
    readonly ConstraintLiteral[] _finalLiterals = new ConstraintLiteral[_state.Variables.Length];

    public Constraint CreateLearnedConstraint(Constraint conflictingConstraint, out ConstraintLiteral uipLiteral, out int jumpBackLevel)
    {
        var variables = _variables;
        var literals = _literals;
        var conflicts = 0;

        Debug.Assert(conflictingConstraint.Literals.All(l => l.Sense == false));

        var learnedLiterals = _learnedLiterals;
        var seenVariables = _seenVariables;
        learnedLiterals.Clear();
        seenVariables.Clear();

        foreach (var literal in conflictingConstraint.Literals)
        {
            seenVariables.Add(literal.Variable.Index);
            learnedLiterals.Add(literal.StampIndex);
            if (literal.Variable.DecisionLevel == _trail.DecisionLevel) conflicts++;
        }

        for (var trailIndex = _trail.Count-1; conflicts > 1; trailIndex--)
        {
            var trailedVariable = _trail[trailIndex];
            _seenVariables.Add(trailedVariable.Index);

            var reason = trailedVariable.Reason;
            if (reason is null) continue;

            var literalToResolve = trailedVariable.Sense == true
                ? trailedVariable.NegativeLiteral
                : trailedVariable.PositiveLiteral;

            if (!learnedLiterals.Remove(literalToResolve.StampIndex)) continue;

            var used = false;
            foreach (var reasonLiteral in reason.Literals)
            {
                if (seenVariables.Contains(reasonLiteral.Variable.Index)) continue;
                if (!learnedLiterals.Add(reasonLiteral.StampIndex)) continue;
                used = true;
                if (reasonLiteral.Variable.DecisionLevel == _trail.DecisionLevel)
                    conflicts++;
            }
            if (used) _activityManager.IncreaseConstraintActivity(reason);

            conflicts--;
        }

        var count = 0;
        foreach (var literal in learnedLiterals.EnumerateIndices().Select(i => literals[i]))
            _finalLiterals[count++] = literal;

        var finalLiterals = _finalLiterals.AsSpan(0, count);

        //_constraintMinimizer.MinimizeConstraint(learnedLiterals, uipLiteral);

        _literalBlockDistanceCounter.Clear();
        jumpBackLevel = 0;
        ConstraintLiteral? uip = null;
        foreach (var literal in finalLiterals)
        {
            var level = literal.Variable.DecisionLevel;
            _literalBlockDistanceCounter.Add(level);
            if (level == _trail.DecisionLevel)
                uip = literal;
            else if (level > jumpBackLevel)
                jumpBackLevel = level;
        }

        Debug.Assert(uip is not null);
        uipLiteral = uip;

        var learnedConstraint = new Constraint(finalLiterals, _activityManager.ConstraintActivityIncrement, _literalBlockDistanceCounter.Count);
        Debug.WriteLine($"Created learned constraint: {learnedConstraint}, uip: {(uipLiteral.Orientation ? "" : "-")}{uipLiteral.Variable.Index+1}.");
        return learnedConstraint;
    }
}
