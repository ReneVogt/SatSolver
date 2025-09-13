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
    TConstraintFactory>(
    SatSolverOptions options,
    ConstraintLiteral[] literals,
    IManageActivities activityManager,
    IVariableTrail trail,
    ITrackPropagationRate propagationRateTracker,
    ITrackLiteralBlockDistance literalBlockDistanceTracker,
    ICreateLearnedConstraints learnedConstraintCreator,
    UnitPropagationQueue unitPropagationQueue,
    IManageRestart restartManager,
    IMinimizeConstraints constraintMinimizer,
    IConstraintFactory constraintFactory,
    Statistics _statistics) : IHandleConflicts
    where TActivityManager : IManageActivities
    where TVariableTrail : IVariableTrail
    where TPropagationRateTracker : ITrackPropagationRate
    where TLiteralBlockDistanceTracker : ITrackLiteralBlockDistance
    where TLearnedConstraintCreator : ICreateLearnedConstraints
    where TRestartManager : IManageRestart
    where TConstraintMinimizer : IMinimizeConstraints        
    where TConstraintFactory : IConstraintFactory
{
    readonly TActivityManager _activityManager = (TActivityManager)activityManager;
    readonly TVariableTrail _trail = (TVariableTrail)trail;
    readonly TPropagationRateTracker _propagationRateTracker = (TPropagationRateTracker)propagationRateTracker;
    readonly TLiteralBlockDistanceTracker _literalBlockDistanceTracker = (TLiteralBlockDistanceTracker)literalBlockDistanceTracker;
    readonly TLearnedConstraintCreator _learnedConstraintCreator = (TLearnedConstraintCreator)learnedConstraintCreator;
    readonly UnitPropagationQueue _unitPropagationQueue = unitPropagationQueue;
    readonly TRestartManager _restartManager = (TRestartManager)restartManager;
    readonly int _literalBlockDistanceDeletionLimit = options.ConstraintDeletion.LiteralBlockDistanceToKeep;
    readonly int _literalBlockDistanceMaximum = options.MaximumLiteralBlockDistance;
    readonly StampArray _learnedLiterals = [];
    readonly ConstraintLiteral[] _literals = literals;
    readonly TConstraintMinimizer _constraintMinimizer = (TConstraintMinimizer)constraintMinimizer;
    readonly TConstraintFactory _constraintFactory = (TConstraintFactory)constraintFactory;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleConflict(Constraint conflictingConstraint)
    {
        _propagationRateTracker.AddConflict();
        _restartManager.AddConflict();
        _activityManager.IncreaseConstraintActivity(conflictingConstraint);
        _unitPropagationQueue.Clear();

        var decisionLevel = _trail.DecisionLevel;

        _learnedConstraintCreator.CreateLearnedConstraint(conflictingConstraint, _learnedLiterals);
        var initialLength = _learnedLiterals.Count;
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

        _statistics.AddConflict(conflictingConstraint, learnedConstraint, initialLength);

        _unitPropagationQueue.Enqueue((learnedConstraint.Watched1, learnedConstraint));
        _trail.JumpBack(jumpBackLevel);

        Assert(learnedConstraint);
    }

    [Conditional("DEBUG")]
    static void Assert(Constraint learnedConstraint)
    {
        var uip = learnedConstraint.Watched1;
        Debug.Assert(learnedConstraint.Literals.All(IsValid));
        Debug.Assert(learnedConstraint.Literals.Contains(uip));

        [ExcludeFromCodeCoverage]
        bool IsValid(ConstraintLiteral literal) =>
            literal == uip && literal.Sense is null ||
            literal!= uip && literal.Sense == false;
    }
}
