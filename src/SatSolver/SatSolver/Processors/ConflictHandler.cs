using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Revo.SatSolver.Processors;

sealed class ConflictHandler<
    TActivityManager, 
    TVariableTrail, 
    TPropagationRateTracker, 
    TLiteralBlockDistanceTracker,
    TLearnedConstraintCreator,
    TRestartManager,
    TConstraintMinimizer> : IHandleConflicts
    where TActivityManager : IManageActivities
    where TVariableTrail : IVariableTrail
    where TPropagationRateTracker : ITrackPropagationRate
    where TLiteralBlockDistanceTracker : ITrackLiteralBlockDistance
    where TLearnedConstraintCreator : ICreateLearnedConstraints
    where TRestartManager : IManageRestart
    where TConstraintMinimizer : IMinimizeConstraints        
{
    readonly TActivityManager _activityManager;
    readonly TVariableTrail _trail;
    readonly TPropagationRateTracker _propagationRateTracker;
    readonly TLiteralBlockDistanceTracker _literalBlockDistanceTracker;
    readonly TLearnedConstraintCreator _learnedConstraintCreator;
    readonly List<Constraint> _learnedConstraints;
    readonly UnitPropagationQueue _unitPropagationQueue;
    readonly TRestartManager _restartManager;
    readonly int _literalBlockDistanceDeletionLimit;
    readonly int _literalBlockDistanceMaximum;
    readonly StampArray _learnedLiterals = [];
    readonly StampArray _literalBlockDistanceCounter = [];
    readonly ConstraintLiteral[] _literals;
    readonly TConstraintMinimizer _constraintMinimizer;

    public ConflictHandler(
        SatSolverOptions options,
        Variable[] variables,
        TActivityManager activityManager,
        TVariableTrail trail,
        TPropagationRateTracker propagationRateTracker,
        TLiteralBlockDistanceTracker literalBlockDistanceTracker,
        TLearnedConstraintCreator learnedConstraintCreator,
        List<Constraint> learnedConstraints,
        UnitPropagationQueue unitPropagationQueue,
        TRestartManager restartManager,
        TConstraintMinimizer constraintMinimizer)
    {
        _literalBlockDistanceDeletionLimit = options.ConstraintDeletion.LiteralBlockDistanceToKeep;
        _literalBlockDistanceMaximum = options.MaximumLiteralBlockDistance;

        _activityManager = activityManager;
        _trail = trail;
        _propagationRateTracker = propagationRateTracker;
        _literalBlockDistanceTracker = literalBlockDistanceTracker;
        _learnedConstraintCreator = learnedConstraintCreator;
        _learnedConstraints = learnedConstraints;
        _unitPropagationQueue = unitPropagationQueue;
        _restartManager = restartManager;
        _constraintMinimizer = constraintMinimizer;

        _literals = new ConstraintLiteral[variables.Length << 1];
        for (var variableIndex = 0; variableIndex < variables.Length; variableIndex++)
        {
            var literalIndex = variableIndex << 1;
            _literals[literalIndex] = variables[variableIndex].PositiveLiteral;
            _literals[literalIndex+1] = variables[variableIndex].NegativeLiteral;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleConflict(Constraint conflictingConstraint)
    {
        _propagationRateTracker.AddConflict();
        _restartManager.AddConflict();
        _activityManager.IncreaseConstraintActivity(conflictingConstraint);
        _unitPropagationQueue.Clear();

        var decisionLevel = _trail.DecisionLevel;

        _learnedConstraintCreator.CreateLearnedConstraint(conflictingConstraint, _learnedLiterals);
        _constraintMinimizer.MinimizeConstraint(_learnedLiterals, decisionLevel, _literals);

        var literals = _literals;
        var finalLiterals = _learnedLiterals.Select(i => literals[i]).ToArray();

        _literalBlockDistanceCounter.Clear();
        var jumpBackLevel = 0;
        ConstraintLiteral? uip = null;
        ConstraintLiteral? secondWatcher = null;
        foreach (var literal in finalLiterals)
        {
            var level = literal.Variable.DecisionLevel;
            _literalBlockDistanceCounter.Add(level);
            if (level == decisionLevel)
                uip = literal;
            else if (level > jumpBackLevel)
            {
                secondWatcher = literal;
                jumpBackLevel = level;
            }
        }
        Debug.Assert(uip is not null);

        var lbd = _literalBlockDistanceCounter.Count;
        var learnedConstraint = new Constraint(
            finalLiterals, 
            uip, 
            secondWatcher ?? uip, 
            _activityManager.ConstraintActivityIncrement, 
            lbd,
            tracked: lbd > _literalBlockDistanceDeletionLimit && lbd <= _literalBlockDistanceMaximum,
            omitted: lbd > _literalBlockDistanceMaximum);

        Statistics.AddLearnedConstraint(learnedConstraint);

        _activityManager.IncreaseVariableActivity(learnedConstraint);
        _activityManager.IncreaseConstraintActivity(learnedConstraint);
        _activityManager.DecayConstraintActivity();

        _literalBlockDistanceTracker.AddLiteralBlockDistance(learnedConstraint.LiteralBlockDistance);

        if (learnedConstraint.IsTracked) _learnedConstraints.Add(learnedConstraint);

        _unitPropagationQueue.Enqueue((uip, learnedConstraint));
        _trail.JumpBack(jumpBackLevel);

        Assert(learnedConstraint, uip);
    }

    [Conditional("DEBUG")]
    [ExcludeFromCodeCoverage]
    static void Assert(Constraint learnedConstraint, ConstraintLiteral uip)
    {
        Debug.Assert(learnedConstraint.Literals.All(l => l == uip && l.Sense is null || l != uip && l.Sense == false));
        Debug.Assert(learnedConstraint.Literals.Contains(uip));
    }
}
