using Revo.SatSolver.DataStructures;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Revo.SatSolver.Tools;

sealed class ConstraintFactory(List<Constraint> _learnedConstraints) : IConstraintFactory    
{
    readonly StampArray _literalBlockDistanceCounter = [];

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Constraint CreateInitialConstraint(IEnumerable<ConstraintLiteral> literals)
    {
        var l = literals.ToArray();
        var watched1 = l[0];
        var watched2 = l.Length > 1 ? l[1] : watched1;
        var constraint = new Constraint(l, watched1, watched2);
        watched1.Watchers.Add(constraint);
        if (watched2 != watched1) watched2.Watchers.Add(constraint);
        if (constraint.Literals.Length == 2)
            Statistics.AddBinaryConstraint();
        return constraint;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Constraint CreateAdditionalConstraint(IEnumerable<ConstraintLiteral> literals)
    {
        var l = literals.ToArray();

        // The first watcher should either be:
        // - a "true" literal with the lowest decision level
        //   (because the constraint will be fulfilled as long
        //   as we don't jump back so far; and if we do jump
        //   this watcher tracks the then unassigned literal)
        // - an unassigned literal if there are no "true" literals
        // - the "false" literal with the highest decision level
        //   (so that it gets unassigned immediatly when jumping
        //   back)
        var firstWatched = l[0];       
        for (var i=1; i<l.Length; i++)
        {
            var literal = l[i];
            var level = literal.Variable.DecisionLevel;

            if (firstWatched.Sense == true)
            {
                if (literal.Sense == true && level < firstWatched.Variable.DecisionLevel)
                    firstWatched = literal;
                continue;
            }

            if (literal.Sense == true)
            {
                firstWatched = literal;
                continue;
            }

            if (firstWatched.Sense is null) continue;
            if (literal.Sense is null || level > firstWatched.Variable.DecisionLevel)
                firstWatched = literal;
        }

        // The second watcher now depends on the state
        // of the first:
        // - first watcher "true"
        //   we look for the literal with
        //   the highest decision level below(!)
        //   the first watchers decision level.
        //   (so after the fulfilling literal is
        //   reset by a back jump, this is the first
        //   to also get unassigned; and before that
        //   we don't need to update watchers during
        //   propagation).
        // - first watcher "false"
        //   we look for the "false" literal with the
        //  (second) highest decision level
        // - first watcher unassigned
        //   we look for either
        //   + an unassigned literal
        //   + the "false" literal with the hightest decision level
        ConstraintLiteral? secondWatched = null;
        for (var i = 0; i<l.Length; i++)
        {
            var literal = l[i];
            if (literal == firstWatched) continue;

            secondWatched ??= literal;
            var level = literal.Variable.DecisionLevel;

            if (firstWatched.Sense == true)
            {
                if ((level > secondWatched.Variable.DecisionLevel ||
                    secondWatched.Variable.DecisionLevel > firstWatched.Variable.DecisionLevel) &&
                    level <= firstWatched.Variable.DecisionLevel)
                    secondWatched = literal;
                continue;
            }

            if (firstWatched.Sense == false)
            {
                // so we know that all literals are false and
                // simply take the one with the hightst decision level
                if (level > secondWatched.Variable.DecisionLevel)
                    secondWatched = literal;
                continue;
            }

            // first watcher is unassigned, so
            // we look for either an unassigned literal
            // or (if the first watcher has the only one)
            // the latest set literal
            if (secondWatched.Sense is null) continue; // that's fine enough
            if (literal.Sense is null)
            {
                secondWatched = literal;
                continue;
            }

            if (level > secondWatched.Variable.DecisionLevel)
                secondWatched = literal;
        }

        var constraint = new Constraint(l, firstWatched, secondWatched ?? firstWatched) { IsAdditional = true };
        constraint.Watched1.Watchers.Add(constraint);
        if (constraint.Watched1 != constraint.Watched2)
            constraint.Watched2.Watchers.Add(constraint);
        LogBinary(constraint);
        return constraint;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Constraint CreateLearnedConstraint(ConstraintLiteral[] learnedLiterals, int decisionLevel, double activity, int maximumLiteralBlockDistance, int literalBlockDistanceDeletionLimit, out int jumpBackLevel)
    {
        _literalBlockDistanceCounter.Clear();
        ConstraintLiteral? uip = null;
        ConstraintLiteral? secondWatcher = null;
        jumpBackLevel = 0;

        for (var i=0; i<learnedLiterals.Length; i++) 
        {
            var literal = learnedLiterals[i];
            var level = literal.Variable.DecisionLevel;
            _literalBlockDistanceCounter.Add(level);
            if (level == decisionLevel)
                uip = literal;
            else if (secondWatcher is null || level > secondWatcher.Variable.DecisionLevel)
            {
                secondWatcher = literal;
                jumpBackLevel = level;
            }
                
        }
        Debug.Assert(uip is not null);

        var lbd = _literalBlockDistanceCounter.Count;
        var learnedConstraint = new Constraint(learnedLiterals, uip, secondWatcher ?? uip)
        {
            IsLearned = true,
            IsOmitted = lbd > maximumLiteralBlockDistance,
            IsTracked = lbd > literalBlockDistanceDeletionLimit && lbd <= maximumLiteralBlockDistance,
            LiteralBlockDistance = lbd,
            Activity = activity
        };
        Statistics.AddLearnedConstraint(learnedConstraint);

        if (!learnedConstraint.IsOmitted)
        {
            learnedConstraint.Watched1.Watchers.Add(learnedConstraint);
            if (learnedConstraint.Watched2 != learnedConstraint.Watched1)
                learnedConstraint.Watched2.Watchers.Add(learnedConstraint);
        }
        if (learnedConstraint.IsTracked)
            _learnedConstraints.Add(learnedConstraint);

        return learnedConstraint;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReleaseLearnedConstraints(double ratio)
    {
        var learnedConstraints = _learnedConstraints;
        var start = (int)(learnedConstraints.Count * (1-ratio));
        for (var i = start; i<learnedConstraints.Count; i++)
            ReleaseConstraint(learnedConstraints[i]);
        learnedConstraints.RemoveRange(start, learnedConstraints.Count-start);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReleaseConstraint(Constraint constraint)
    {
        if (constraint.IsOmitted) return;
        constraint.IsTracked = false;
        constraint.Watched1.Watchers.Remove(constraint);
        constraint.Watched2.Watchers.Remove(constraint);
    }

    [Conditional("DEBUG")]
    [ExcludeFromCodeCoverage]
    static void LogBinary(Constraint constraint)
    {
        if (constraint.Literals.Length == 2)
            Statistics.AddBinaryConstraint();
    }
}
