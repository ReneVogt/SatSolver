using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;
using System.Diagnostics;

namespace Revo.SatSolver.Processors;

sealed class ConflictHandler(
    SatSolverOptions _options, 
    IManageActivities _activityManager, 
    IVariableTrail _trail,
    ITrackPropagationRate _propagationRateTracker, 
    ITrackLiteralBlockDistance _literalBlockDistanceTracker,
    ICreateLearnedConstraints _learnedConstraintCreator,
    List<Constraint> _learnedConstraints,
    UnitPropagationQueue _unitPropagationQueue,
    IManageRestart _restartManager) : IHandleConflicts
{
    readonly int _literalBlockDistanceDeletionLimit = _options.ConstraintDeletion.LiteralBlockDistanceToKeep;
    readonly int _literalBlockDistanceMaximum = _options.MaximumLiteralBlockDistance;

    public void HandleConflict(Constraint conflictingConstraint)
    {
        _propagationRateTracker.AddConflict();
        _restartManager.AddConflict();
        _activityManager.IncreaseConstraintActivity(conflictingConstraint);
        _unitPropagationQueue.Clear();

        var learnedConstraint = _learnedConstraintCreator.CreateLearnedConstraint(conflictingConstraint, out var uipLiteral, out var jumpBackLevel);
        _activityManager.IncreaseVariableActivity(learnedConstraint);

        _unitPropagationQueue.Enqueue((uipLiteral, learnedConstraint));

        if (learnedConstraint.LiteralBlockDistance > _literalBlockDistanceMaximum)
        {
            Statistics.AddOmittedLearnedConstraint();
            Debug.WriteLine($"LBD {learnedConstraint.LiteralBlockDistance} too high, only jumping back.");
            _trail.JumpBack(jumpBackLevel);
            return;
        }

        // If the learned constraint has an lbd so low that
        // we will never remove it, we don't need to track
        // it.
        if (learnedConstraint.LiteralBlockDistance > _literalBlockDistanceDeletionLimit)
        {
            Statistics.AddTrackedLearnedConstraint();
            Debug.WriteLine($"LBD {learnedConstraint.LiteralBlockDistance}, we track this constraint to eventually delete it.");
            _activityManager.IncreaseConstraintActivity(learnedConstraint);
            _learnedConstraints.Add(learnedConstraint);
            learnedConstraint.IsTracked = true;
        }
        else
        {
            Statistics.AddPermanentLearnedConstraint();
            Debug.WriteLine($"LBD {learnedConstraint.LiteralBlockDistance} so good, we keep this forever.");
        }

        _trail.JumpBack(jumpBackLevel);
        _activityManager.DecayConstraintActivity();
        _literalBlockDistanceTracker.AddValue(learnedConstraint.LiteralBlockDistance);
        Debug.Assert(learnedConstraint.Literals.All(l => l == uipLiteral && l.Sense is null || l != uipLiteral && l.Sense == false));
        Debug.Assert(learnedConstraint.Literals.Contains(uipLiteral));
    }
}
