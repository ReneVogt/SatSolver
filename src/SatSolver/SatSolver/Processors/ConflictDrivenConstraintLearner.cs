using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
using Revo.SatSolver.Tools;
using System.Diagnostics;

namespace Revo.SatSolver.Processors;

sealed class ConflictDrivenConstraintLearner(SatSolverState _state) : IConflictDrivenConstraintLearner
{
    readonly int _literalBlockDistanceDeletionLimit = _state.Options.ConstraintDeletion.LiteralBlockDistanceToKeep;
    readonly int _literalBlockDistanceMaximum = _state.Options.MaximumLiteralBlockDistance;
    readonly IActivityManager _activityManager = _state.ActivityManager;
    readonly IVariableTrail _trail = _state.VariableTrail;
    readonly EmaTracker _literalBlockDistanceTracker = _state.LiteralBlockDistanceTracker;
    readonly ICreateLearnedConstraints _learnedConstraintCreator = _state.LearnedConstraintCreator;
    readonly List<Constraint> _learnedConstraints = _state.LearnedConstraints;

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

        learnedConstraint.Watched1.Watchers.Add(learnedConstraint);
        if (learnedConstraint.Watched2 != learnedConstraint.Watched1)
            learnedConstraint.Watched2.Watchers.Add(learnedConstraint);

        _trail.JumpBack(jumpBackLevel);
        _activityManager.DecayConstraintActivity();
        _literalBlockDistanceTracker.AddValue(learnedConstraint.LiteralBlockDistance);
        Debug.Assert(learnedConstraint.Literals.All(l => l == uipLiteral && l.Sense is null || l != uipLiteral && l.Sense == false));
        Debug.Assert(learnedConstraint.Literals.Contains(uipLiteral));
        return (uipLiteral, learnedConstraint);
    }
}
