using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;

namespace Revo.SatSolver.Tools;

sealed class RestartManager : IManageRestart
{
    readonly IVariableTrail _trail;
    readonly ITrackPropagationRate _propagationRateTracker;
    readonly ITrackLiteralBlockDistance _literalBlockDistanceTracker;
    readonly UnitPropagationQueue _unitPropagationQueue;
    readonly ILubySequence? _lubySequence;

    readonly bool _useRestarts, _restartOnPropagationRate, _restartOnLiteralBlockDistance;
    readonly IReduceLearnedConstraints _constraintReducer;
    readonly bool _reduceConstraints;

    long _restartCounter, _nextRestartThreshold;

    public RestartManager(
        SatSolverOptions options,
        IVariableTrail trail,
        ITrackPropagationRate propagationRateTracker,
        ITrackLiteralBlockDistance literalBlockDistanceTracker,
        UnitPropagationQueue unitPropagationQueue,
        IReduceLearnedConstraints constraintReducer,
        ILubySequence? lubySequence)
    {
        _trail = trail;
        _propagationRateTracker = propagationRateTracker;
        _literalBlockDistanceTracker = literalBlockDistanceTracker;
        _unitPropagationQueue = unitPropagationQueue;
        _constraintReducer = constraintReducer;

        _lubySequence = lubySequence;
        _nextRestartThreshold = _lubySequence?.Next() ?? options.Restart.Interval ?? long.MaxValue;
        _reduceConstraints = options.ConstraintDeletion.ReduceOnRestart;
        _restartOnPropagationRate = options.Restart.ByPropagationRate;
        _restartOnLiteralBlockDistance = options.Restart.ByLiteralBlockDistance;
        _useRestarts = options.Restart.Interval is not null  ||
            _lubySequence is not null ||
            _restartOnPropagationRate ||
            _restartOnLiteralBlockDistance;
    }

    public void AddConflict() => _restartCounter++;
    public bool RestartIfNecessary()
    {
        if (!_useRestarts) return false;

        if (!(_restartCounter >= _nextRestartThreshold || 
            _restartOnPropagationRate && _propagationRateTracker.ShouldRestart() || 
            _restartOnLiteralBlockDistance && _literalBlockDistanceTracker.ShouldRestart())) return false;

        Statistics.LogRestart(_restartCounter, _nextRestartThreshold, _propagationRateTracker.CurrentRatio, _literalBlockDistanceTracker.CurrentRatio);

        _restartCounter = 0;
        if (_lubySequence is not null)
            _nextRestartThreshold = _lubySequence.Next();

        _trail.JumpBack(0);
        _unitPropagationQueue.Clear();
        _propagationRateTracker.ResetAfterRestart();
        _literalBlockDistanceTracker.ResetAfterRestart();

        if (_reduceConstraints)
            _constraintReducer.ReduceLearnedConstraints();

        return true;
    }
}
