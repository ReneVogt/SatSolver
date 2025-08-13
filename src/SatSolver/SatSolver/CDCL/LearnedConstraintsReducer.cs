using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;

namespace Revo.SatSolver.CDCL;

sealed class LearnedConstraintsReducer(SatSolver.Options _options, PropagationRateTracker? _propagationRateTracker, EmaTracker? _literalBlockDistanceTracker, List<Constraint> _learnedConstraints, int _originalClauseCount) : IReduceLearnedConstraints
{
    readonly double _originalClauseCountFactor = _options.ClauseDeletion?.OriginalClauseCountFactor ?? double.MaxValue;
    readonly double _propagationRateThreshold = _options.ClauseDeletion?.PropagationRateThreshold ?? 0;
    readonly double _literalBlockDistanceThreshold = _options.ClauseDeletion?.LiteralBlockDistanceThreshold ?? 0;
    readonly double _ratioToDelete = _options.ClauseDeletion?.RatioToDelete ?? 0;
    readonly bool _reduceClauses = _options.ClauseDeletion?.OriginalClauseCountFactor is not null ||
        _options.ClauseDeletion?.PropagationRateThreshold is not null ||
        _options.ClauseDeletion?.LiteralBlockDistanceThreshold is not null;

    public void ReduceLearnedConstraintsIfNecessary()
    {
        if (!_reduceClauses) return;

        // reduce clauses if we learned too many already
        var reduce = _learnedConstraints.Count > _originalClauseCount * _originalClauseCount;
        // or if the propagation rate is too low
        reduce |= _propagationRateTracker?.CurrentRatio < _propagationRateThreshold;
        // or if the literal block distance average is too high
        reduce |= _literalBlockDistanceTracker?.CurrentRatio > _literalBlockDistanceThreshold;
        
        if (!reduce) return;

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
    }
}