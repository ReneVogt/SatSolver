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
    public Constraint CreateFromSoluution<TVariableTrail>(Variable[] variables, TVariableTrail trail, double activity) where TVariableTrail : IVariableTrail
    {
        var literals = variables.Select(variable => variable.Sense!.Value ? variable.NegativeLiteral : variable.PositiveLiteral).ToArray();
        var trailedVariable = trail[^1];
        var firstWatched = trailedVariable.Sense!.Value ? trailedVariable.NegativeLiteral : trailedVariable.PositiveLiteral;
        var secondWatched = firstWatched;
        if (trail.Count > 1)
        {
            trailedVariable = trail[^2];
            secondWatched = trailedVariable.Sense!.Value ? trailedVariable.NegativeLiteral : trailedVariable.PositiveLiteral;
        }

        var constraint = new Constraint(literals, firstWatched, secondWatched) { Activity = activity, IsLearned = true };
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
        {
            var constraint = learnedConstraints[i];
            constraint.IsTracked = false;
            constraint.Watched1.Watchers.Remove(constraint);
            constraint.Watched2.Watchers.Remove(constraint);
        }
        learnedConstraints.RemoveRange(start, learnedConstraints.Count-start);
    }

    [Conditional("DEBUG")]
    [ExcludeFromCodeCoverage]
    static void LogBinary(Constraint constraint)
    {
        if (constraint.Literals.Length == 2)
            Statistics.AddBinaryConstraint();
    }
}
