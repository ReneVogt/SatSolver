using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;

namespace Revo.SatSolver.CDCL;

sealed class CdclProcessor(SatSolver.Options _options, IActivityManager _activityManager, IVariableTrail _trail, EmaTracker? _literalBlockDistanceTracker, ICreateLearnedConstraints _learnedConstraintCreator, List<Constraint> _learnedConstraints)
{
    readonly int _literalBlockDistanceDeletionLimit = _options.ClauseDeletion?.LiteralBlockDistanceToKeep ?? int.MaxValue;
    readonly int _literalBlockDistanceMaximum = _options.MaximumLiteralBlockDistance;

    public ConstraintLiteral PerformClauseLearning(Constraint conflictingConstraint)
    {
        var learnedConstraint = _learnedConstraintCreator.CreateLearnedConstraint(conflictingConstraint, out var uipLiteral, out var jumpBackLevel);
        _activityManager.IncreaseVariableActivity(learnedConstraint);

        if (learnedConstraint.LiteralBlockDistance > _literalBlockDistanceMaximum)
        {
            _trail.JumpBack(jumpBackLevel);
            return uipLiteral;
        }

        // If the learned constraint as an lbd so low that
        // we will never remove it, we don't need to track
        // it.
        if (learnedConstraint.LiteralBlockDistance > _literalBlockDistanceDeletionLimit)
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

        _trail.JumpBack(jumpBackLevel);
        _literalBlockDistanceTracker?.AddValue(learnedConstraint.LiteralBlockDistance);
        return uipLiteral;
    }
}
