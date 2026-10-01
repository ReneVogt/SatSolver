using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;
using System.Diagnostics;

namespace Revo.SatSolver.Processors;

sealed class LearnedConstraintCreator(IVariableTrail _trail, IManageActivities _activityManager) : ICreateLearnedConstraints
{
    readonly StampArray _seenVariables = [];

    public void CreateLearnedConstraint(Constraint conflictingConstraint, StampArray learnedLiterals)
    {
        var conflicts = 0;
        var decisionLevel = _trail.DecisionLevel;

        Debug.Assert(conflictingConstraint.Literals.All(l => l.Sense == false));

        var seenVariables = _seenVariables;
        learnedLiterals.Clear();
        seenVariables.Clear();

        foreach (var literal in conflictingConstraint.Literals)
        {
            seenVariables.Add(literal.Variable.Index);
            learnedLiterals.Add(literal.StampIndex);
            if (literal.Variable.DecisionLevel == decisionLevel) conflicts++;
        }

        for (var trailIndex = _trail.Count-1; conflicts > 1; trailIndex--)
        {
            var trailedVariable = _trail[trailIndex];
            _seenVariables.Add(trailedVariable.Index);

            var reason = trailedVariable.Reason;

            // the reason cannot be null, because
            // this is only true for the decision
            // literal on this decision level, and
            // that could only happen if this already
            // is the last "conflict" in the constraint,
            // so we would have exited the loop already.
            Debug.Assert(reason is not null); 

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
                if (reasonLiteral.Variable.DecisionLevel == decisionLevel)
                    conflicts++;
            }
            if (used) _activityManager.IncreaseConstraintActivity(reason);

            conflicts--;
        }
    }
}
