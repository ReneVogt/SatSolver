using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Revo.SatSolver.Processors;

sealed class ConflictHandler : IHandleConflicts
{
    readonly IManageActivities _activityManager;
    readonly IVariableTrail _trail;
    readonly ITrackPropagationRate _propagationRateTracker;
    readonly ITrackLiteralBlockDistance _literalBlockDistanceTracker;
    readonly ICreateLearnedConstraints _learnedConstraintCreator;
    readonly List<Constraint> _learnedConstraints;
    readonly UnitPropagationQueue _unitPropagationQueue;
    readonly IManageRestart _restartManager;
    readonly int _literalBlockDistanceDeletionLimit;
    readonly int _literalBlockDistanceMaximum;
    readonly StampArray _learnedLiterals = [];
    readonly StampArray _literalBlockDistanceCounter = [];
    readonly ConstraintLiteral[] _literals;
    readonly IMinimizeConstraints _constraintMinimizer;

    public ConflictHandler(
        SatSolverOptions options,
        Variable[] variables,
        IManageActivities activityManager,
        IVariableTrail trail,
        ITrackPropagationRate propagationRateTracker,
        ITrackLiteralBlockDistance literalBlockDistanceTracker,
        ICreateLearnedConstraints learnedConstraintCreator,
        List<Constraint> learnedConstraints,
        UnitPropagationQueue unitPropagationQueue,
        IManageRestart restartManager,
        IMinimizeConstraints constraintMinimizer)
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

    public void HandleConflict(Constraint conflictingConstraint)
    {
        _propagationRateTracker.AddConflict();
        _restartManager.AddConflict();
        _activityManager.IncreaseConstraintActivity(conflictingConstraint);
        _unitPropagationQueue.Clear();

        _learnedConstraintCreator.CreateLearnedConstraint(conflictingConstraint, _learnedLiterals);
        //_constraintMinimizer.MinimizeConstraint(learnedLiterals, uipLiteral);

        var literals = _literals;
        var finalLiterals = _learnedLiterals.EnumerateIndices().Select(i => literals[i]).ToArray();

        _literalBlockDistanceCounter.Clear();
        var jumpBackLevel = 0;
        var decisionLevel = _trail.DecisionLevel;
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

        _literalBlockDistanceTracker.AddValue(learnedConstraint.LiteralBlockDistance);

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
