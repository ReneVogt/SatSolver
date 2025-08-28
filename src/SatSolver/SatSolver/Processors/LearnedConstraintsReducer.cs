using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Revo.SatSolver.Processors;

sealed class LearnedConstraintsReducer(
    SatSolverOptions _options, 
    List<Constraint> _learnedConstraints, 
    int _originalConstraintCount) : IReduceLearnedConstraints
{
    readonly double _originalConstraintCountFactor = _options.ConstraintDeletion.OriginalConstraintCountFactor ?? double.MaxValue;
    readonly int _conflictInterval = _options.ConstraintDeletion.ConflictInterval ?? int.MaxValue;
    readonly double _ratioToDelete = _options.ConstraintDeletion.RatioToDelete;
    readonly bool _reduceClauses = _options.ConstraintDeletion.RatioToDelete > 0 && (_options.ConstraintDeletion.OriginalConstraintCountFactor is not null ||
        _options.ConstraintDeletion.ConflictInterval is not null);

    int _conflictCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReduceLearnedConstraintsIfNecessary()
    {
        if (!_reduceClauses) return;

        _conflictCount++;

        // reduce clauses if we learned too many already
        var reduce = _learnedConstraints.Count > _originalConstraintCount * _originalConstraintCountFactor;
        // or if the literal block distance is too high
        reduce |= _conflictCount >= _conflictInterval;

        if (reduce) ReduceLearnedConstraints();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReduceLearnedConstraints()
    {
        var previousCount = _learnedConstraints.Count;
        Debug.WriteLine($"Start reducing learned constraints (currently {previousCount}, factor {previousCount/(double)_originalConstraintCount}): conflicts {_conflictCount} / {_conflictInterval}.");
        _conflictCount = 0;

        var learnedConstraints = _learnedConstraints;
        learnedConstraints.Sort((left, right) =>
            (left.LiteralBlockDistance, -left.Activity, left.Literals.Length)
            .CompareTo((right.LiteralBlockDistance, -right.Activity, right.Literals.Length)));
        var start = (int)(_learnedConstraints.Count * (1-_ratioToDelete));
        for (var i = start; i<learnedConstraints.Count; i++)
        {
            var constraint = learnedConstraints[i];
            constraint.IsTracked = false;
            constraint.Watched1.Watchers.Remove(constraint);
            constraint.Watched2.Watchers.Remove(constraint);
        }
        learnedConstraints.RemoveRange(start, learnedConstraints.Count-start);
        var countReduced = _learnedConstraints.Count;
        Debug.WriteLine($"Reduced learned constraints to {countReduced}.");
        Statistics.AddReducedLearnedConstraint(previousCount - countReduced);
    }
}