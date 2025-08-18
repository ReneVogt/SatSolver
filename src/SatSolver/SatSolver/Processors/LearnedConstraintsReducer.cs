using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;
using System.Diagnostics;

namespace Revo.SatSolver.Processors;

sealed class LearnedConstraintsReducer(SatSolverState _state) : IReduceLearnedConstraints
{
    readonly double _originalConstraintCountFactor = _state.Options.ConstraintDeletion.OriginalConstraintCountFactor ?? double.MaxValue;
    readonly double _propagationRateThreshold = _state.Options.ConstraintDeletion.PropagationRateThreshold ?? 0;
    readonly double _literalBlockDistanceThreshold = _state.Options.ConstraintDeletion.LiteralBlockDistanceThreshold ?? double.MaxValue;
    readonly double _ratioToDelete = _state.Options.ConstraintDeletion.RatioToDelete;
    readonly bool _reduceClauses = _state.Options.ConstraintDeletion.RatioToDelete > 0 && (_state.Options.ConstraintDeletion.OriginalConstraintCountFactor is not null ||
        _state.Options.ConstraintDeletion.PropagationRateThreshold is not null ||
        _state.Options.ConstraintDeletion.LiteralBlockDistanceThreshold is not null);
    readonly List<Constraint> _learnedConstraints = _state.LearnedConstraints;
    readonly int _originalConstraintCount = _state.OriginalConstraintCount;
    readonly PropagationRateTracker _propagationRateTracker = _state.PropagationRateTracker;
    readonly EmaTracker _literalBlockDistanceTracker = _state.LiteralBlockDistanceTracker;

    public void ReduceLearnedConstraintsIfNecessary()
    {
        if (!_reduceClauses) return;

        // reduce clauses if we learned too many already
        var reduce = _learnedConstraints.Count > _originalConstraintCount * _originalConstraintCountFactor;
        // or if the propagation rate is too low
        reduce |= _propagationRateTracker.CurrentRatio < _propagationRateThreshold;
        // or if the literal block distance is too high
        reduce |= _literalBlockDistanceTracker.CurrentRatio > _literalBlockDistanceThreshold;
        
        if (!reduce) return;

        Debug.WriteLine($"Start reducing learned constraints ({_learnedConstraints.Count}).");

        var learnedConstraints = _learnedConstraints;
        learnedConstraints.Sort((left, right) => -left.Activity.CompareTo(right.Activity));
        var start = (int)(_learnedConstraints.Count * _ratioToDelete);
        for (var i = start; i<learnedConstraints.Count; i++)
        {
            var constraint = learnedConstraints[i];
            constraint.IsTracked = false;
            constraint.Watched1.Watchers.Remove(constraint);
            constraint.Watched2.Watchers.Remove(constraint);
        }
        learnedConstraints.RemoveRange(start, learnedConstraints.Count-start);
        Debug.WriteLine($"Reduced learned constraints to {_learnedConstraints.Count}.");
    }
}