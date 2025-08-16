using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
using System.Diagnostics;

namespace Revo.SatSolver.CDCL;

sealed class CdclProcessor(SatSolver.Options _options, IActivityManager _activityManager, IVariableTrail _trail, EmaTracker _literalBlockDistanceTracker, ICreateLearnedConstraints _learnedConstraintCreator, List<Constraint> _learnedConstraints)
{
    readonly int _literalBlockDistanceDeletionLimit = _options.ClauseDeletion.LiteralBlockDistanceToKeep;
    readonly int _literalBlockDistanceMaximum = _options.MaximumLiteralBlockDistance;

    public (ConstraintLiteral uip, Constraint reason) PerformClauseLearning(Constraint conflictingConstraint)
    {
        var learnedConstraint = _learnedConstraintCreator.CreateLearnedConstraint(conflictingConstraint, out var uipLiteral, out var jumpBackLevel);
        _activityManager.IncreaseVariableActivity(learnedConstraint);

        if (learnedConstraint.LiteralBlockDistance > _literalBlockDistanceMaximum)
        {
            Debug.WriteLine($"LBD {learnedConstraint.LiteralBlockDistance} too high, only jumping back.");
            _trail.JumpBack(jumpBackLevel);
            return (uipLiteral, learnedConstraint);
        }

        // If the learned constraint as an lbd so low that
        // we will never remove it, we don't need to track
        // it.
        if (learnedConstraint.LiteralBlockDistance > _literalBlockDistanceDeletionLimit)
        {
            Debug.WriteLine($"LBD {learnedConstraint.LiteralBlockDistance}, we track this constraint to eventually delete it.");
            _activityManager.IncreaseConstraintActivity(learnedConstraint);
            _learnedConstraints.Add(learnedConstraint);
            learnedConstraint.IsTracked = true;
        }
        else
            Debug.WriteLine($"LBD {learnedConstraint.LiteralBlockDistance} so good, we keep this forever.");

        learnedConstraint.Watched1 = uipLiteral;
        uipLiteral.Watchers.Add(learnedConstraint);
        if (learnedConstraint.Literals.Length == 1)
            learnedConstraint.Watched2 = uipLiteral;
        else
        {
            // it is important to set the watcher to the literal
            // that was assigned just before the uip to avoid
            // it watching a false literal while there are unassigned
            // literals after another backjump
            learnedConstraint.Watched2 = learnedConstraint.Literals.Where(l => l != uipLiteral).MaxBy(l => l.Variable.DecisionLevel)!;
            learnedConstraint.Watched2.Watchers.Add(learnedConstraint);
        }

        _trail.JumpBack(jumpBackLevel);
        _activityManager.DecayConstraintActivity();
        _literalBlockDistanceTracker.AddValue(learnedConstraint.LiteralBlockDistance);
        Debug.Assert(learnedConstraint.Literals.All(l => l == uipLiteral && l.Sense is null || l != uipLiteral && l.Sense == false));
        Debug.Assert(learnedConstraint.Literals.Contains(uipLiteral));
        return (uipLiteral, learnedConstraint);
    }
}
