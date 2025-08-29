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
    TConstraintMinimizer,
    TConstraintFactory> : IHandleConflicts
    where TActivityManager : IManageActivities
    where TVariableTrail : IVariableTrail
    where TPropagationRateTracker : ITrackPropagationRate
    where TLiteralBlockDistanceTracker : ITrackLiteralBlockDistance
    where TLearnedConstraintCreator : ICreateLearnedConstraints
    where TRestartManager : IManageRestart
    where TConstraintMinimizer : IMinimizeConstraints        
    where TConstraintFactory : IConstraintFactory
{
    readonly TActivityManager _activityManager;
    readonly TVariableTrail _trail;
    readonly TPropagationRateTracker _propagationRateTracker;
    readonly TLiteralBlockDistanceTracker _literalBlockDistanceTracker;
    readonly TLearnedConstraintCreator _learnedConstraintCreator;
    readonly UnitPropagationQueue _unitPropagationQueue;
    readonly TRestartManager _restartManager;
    readonly int _literalBlockDistanceDeletionLimit;
    readonly int _literalBlockDistanceMaximum;
    readonly StampArray _learnedLiterals = [];
    readonly ConstraintLiteral[] _literals;
    readonly TConstraintMinimizer _constraintMinimizer;
    readonly TConstraintFactory _constraintFactory;

    public ConflictHandler(
        SatSolverOptions options,
        Variable[] variables,
        TActivityManager activityManager,
        TVariableTrail trail,
        TPropagationRateTracker propagationRateTracker,
        TLiteralBlockDistanceTracker literalBlockDistanceTracker,
        TLearnedConstraintCreator learnedConstraintCreator,
        UnitPropagationQueue unitPropagationQueue,
        TRestartManager restartManager,
        TConstraintMinimizer constraintMinimizer,
        TConstraintFactory constraintFactory)
    {
        _literalBlockDistanceDeletionLimit = options.ConstraintDeletion.LiteralBlockDistanceToKeep;
        _literalBlockDistanceMaximum = options.MaximumLiteralBlockDistance;

        _activityManager = activityManager;
        _trail = trail;
        _propagationRateTracker = propagationRateTracker;
        _literalBlockDistanceTracker = literalBlockDistanceTracker;
        _learnedConstraintCreator = learnedConstraintCreator;
        _unitPropagationQueue = unitPropagationQueue;
        _restartManager = restartManager;
        _constraintMinimizer = constraintMinimizer;
        _constraintFactory = constraintFactory;

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

        var learnedConstraint = _constraintFactory.CreateLearnedConstraint(
            finalLiterals, 
            decisionLevel, 
            _activityManager.ConstraintActivityIncrement, 
            _literalBlockDistanceMaximum, 
            _literalBlockDistanceDeletionLimit,
            out var jumpBackLevel);

        _activityManager.IncreaseVariableActivity(learnedConstraint);
        _activityManager.IncreaseConstraintActivity(learnedConstraint);
        _activityManager.DecayConstraintActivity();

        _literalBlockDistanceTracker.AddLiteralBlockDistance(learnedConstraint.LiteralBlockDistance);

        _unitPropagationQueue.Enqueue((learnedConstraint.Watched1, learnedConstraint));
        _trail.JumpBack(jumpBackLevel);

        Assert(learnedConstraint);
    }

    [Conditional("DEBUG")]
    [ExcludeFromCodeCoverage]
    static void Assert(Constraint learnedConstraint)
    {
        var uip = learnedConstraint.Watched1;
        Debug.Assert(learnedConstraint.Literals.All(l => l == uip && l.Sense is null || l != uip && l.Sense == false));
        Debug.Assert(learnedConstraint.Literals.Contains(uip));
    }
}
