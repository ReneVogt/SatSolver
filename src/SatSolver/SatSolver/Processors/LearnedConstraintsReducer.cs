using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Revo.SatSolver.Processors;

sealed class LearnedConstraintsReducer<TConstraintFactory>(
    SatSolverOptions _options, 
    List<Constraint> _learnedConstraints, 
    IConstraintFactory constraintFactory,
    Statistics _statistics) : IReduceLearnedConstraints
    where TConstraintFactory : IConstraintFactory
{
    readonly TConstraintFactory _constraintFactory = (TConstraintFactory)constraintFactory;
    readonly double _originalConstraintCountFactor = _options.ConstraintDeletion.OriginalConstraintCountFactor ?? double.MaxValue;
    readonly int _conflictInterval = _options.ConstraintDeletion.ConflictInterval ?? int.MaxValue;
    readonly double _ratioToDelete = _options.ConstraintDeletion.RatioToDelete;
    readonly bool _reduceClauses = _options.ConstraintDeletion.RatioToDelete > 0 && (_options.ConstraintDeletion.OriginalConstraintCountFactor is not null ||
        _options.ConstraintDeletion.ConflictInterval is not null);

    int _conflictCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReduceLearnedConstraintsIfNecessary(int originalConstraintCount)
    {
        if (!_reduceClauses) return;

        _conflictCount++;

        // reduce clauses if we learned too many already
        var reduce = _learnedConstraints.Count > originalConstraintCount * _originalConstraintCountFactor;
        // or if the literal block distance is too high
        reduce |= _conflictCount >= _conflictInterval;

        if (reduce) ReduceLearnedConstraints();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReduceLearnedConstraints()
    {
        var previousCount = _learnedConstraints.Count;
        var learnedConstraints = _learnedConstraints;
        learnedConstraints.Sort((left, right) =>
            (left.LiteralBlockDistance, -left.Activity, left.Literals.Length)
            .CompareTo((right.LiteralBlockDistance, -right.Activity, right.Literals.Length)));
        _constraintFactory.ReleaseLearnedConstraints(_ratioToDelete);
        var countReduced = _learnedConstraints.Count;

        _statistics.LogConstraintDeletion(previousCount, previousCount - countReduced, _conflictCount, _conflictInterval);
        _conflictCount = 0;
    }
}