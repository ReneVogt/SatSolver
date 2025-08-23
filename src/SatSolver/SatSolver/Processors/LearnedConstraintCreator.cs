using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;
using System.Diagnostics;

namespace Revo.SatSolver.Processors;

sealed class LearnedConstraintCreator : ICreateLearnedConstraints
{
    readonly StampArray _literalBlockDistanceCounter = new();
    readonly StampArray _learnedLiterals = new();
    readonly StampArray _seenVariables = new();
    readonly ConstraintLiteral[] _literals;
    readonly IVariableTrail _trail;
    readonly IManageActivities _activityManager;
    readonly IMinimizeConstraints _constraintMinimizer;
    readonly Variable[] _variables;
    readonly int _maximumLiteralBlockDistance;

    public LearnedConstraintCreator(IVariableTrail trail, IManageActivities activityManager, IMinimizeConstraints constraintMinimizer, Variable[] variables, int maximumLiteralBlockDistance)
    {
        _trail = trail;
        _activityManager = activityManager;
        _constraintMinimizer = constraintMinimizer;
        _variables = variables;
        _maximumLiteralBlockDistance = maximumLiteralBlockDistance;

        _literals = new ConstraintLiteral[_variables.Length << 1];
        for (var variableIndex = 0; variableIndex < _variables.Length; variableIndex++)
        {
            var literalIndex = variableIndex << 1;
            _literals[literalIndex] = _variables[variableIndex].PositiveLiteral;
            _literals[literalIndex+1] = _variables[variableIndex].NegativeLiteral;
        }
    }
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

        var finalLiterals = learnedLiterals.EnumerateIndices().Select(i => literals[i]).ToArray();

        //_constraintMinimizer.MinimizeConstraint(learnedLiterals, uipLiteral);

        _literalBlockDistanceCounter.Clear();
        jumpBackLevel = -1;
        ConstraintLiteral? uip = null;
        ConstraintLiteral? secondWatcher = null;
        foreach (var literal in finalLiterals)
        {
            var level = literal.Variable.DecisionLevel;
            _literalBlockDistanceCounter.Add(level);
            if (level == _trail.DecisionLevel)
                uip = literal;
            else if (level > jumpBackLevel)
            {
                secondWatcher = literal;
                jumpBackLevel = level;
            }
        }
        if (jumpBackLevel < 0) jumpBackLevel = 0;
        Debug.Assert(uip is not null);
        uipLiteral = uip;

        var lbd = _literalBlockDistanceCounter.Count;
        var learnedConstraint = new Constraint(finalLiterals, uip, secondWatcher ?? uip, _activityManager.ConstraintActivityIncrement, lbd, lbd <= _maximumLiteralBlockDistance);
        Debug.WriteLine($"Created learned constraint: {learnedConstraint}, uip: {(uipLiteral.Orientation ? "" : "-")}{uipLiteral.Variable.Index+1}.");
        return learnedConstraint;
    }
}
