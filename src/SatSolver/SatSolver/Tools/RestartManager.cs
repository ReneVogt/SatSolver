using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Revo.SatSolver.Tools;

sealed class RestartManager<
        TVariableTrail,
        TPropagationRateTracker,
        TLiteralBlockDistanceTracker,
        TLearnedConstraintReducer,
        TLubySequence> : IManageRestart
    where TVariableTrail : IVariableTrail
    where TPropagationRateTracker : ITrackPropagationRate
    where TLiteralBlockDistanceTracker : ITrackLiteralBlockDistance
    where TLearnedConstraintReducer : IReduceLearnedConstraints
    where TLubySequence : ILubySequence
{
    readonly TVariableTrail _trail;
    readonly TPropagationRateTracker _propagationRateTracker;
    readonly TLiteralBlockDistanceTracker _literalBlockDistanceTracker;
    readonly UnitPropagationQueue _unitPropagationQueue;
    readonly TLubySequence? _lubySequence;

    readonly bool _useRestarts, _restartOnPropagationRate, _restartOnLiteralBlockDistance;
    readonly TLearnedConstraintReducer _constraintReducer;
    readonly bool _reduceConstraints;

    long _restartCounter, _nextRestartThreshold;

    public RestartManager(
        TVariableTrail trail,
        TPropagationRateTracker propagationRateTracker,
        TLiteralBlockDistanceTracker literalBlockDistanceTracker,
        UnitPropagationQueue unitPropagationQueue,
        TLearnedConstraintReducer constraintReducer,
        int? restartInterval, TLubySequence? lubySequence,
        bool restartOnPropagationRate, bool restartOnLiteralBlockDistance,
        bool reduceConstraints)
    {
        _trail = trail;
        _propagationRateTracker = propagationRateTracker;
        _literalBlockDistanceTracker = literalBlockDistanceTracker;
        _unitPropagationQueue = unitPropagationQueue;
        _constraintReducer = constraintReducer;

        _lubySequence = lubySequence;
        _nextRestartThreshold = _lubySequence?.Next() ?? restartInterval ?? long.MaxValue;
        _reduceConstraints = reduceConstraints;
        _restartOnPropagationRate = restartOnPropagationRate;
        _restartOnLiteralBlockDistance = restartOnLiteralBlockDistance;
        _useRestarts = restartInterval is not null  ||
            _lubySequence is not null ||
            _restartOnPropagationRate ||
            _restartOnLiteralBlockDistance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddConflict() => _restartCounter++;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RestartIfNecessary()
    {
        if (!_useRestarts) return false;

        if (!(_restartCounter > _nextRestartThreshold || 
            _restartOnPropagationRate && _propagationRateTracker.ShouldRestart() || 
            _restartOnLiteralBlockDistance && _literalBlockDistanceTracker.ShouldRestart())) return false;

        Debug.WriteLine($"Restarting (counter: {_restartCounter} / {_nextRestartThreshold}, propagation rate: {_propagationRateTracker.CurrentRatio}, lbd: {_literalBlockDistanceTracker.CurrentRatio}).");

        _restartCounter = 0;
        if (_lubySequence is not null)
            _nextRestartThreshold = _lubySequence.Next();

        _trail.JumpBack(0);
        _unitPropagationQueue.Clear();
        _propagationRateTracker.ResetAfterRestart();
        _literalBlockDistanceTracker.ResetAfterRestart();

        if (_reduceConstraints)
        {
            Debug.WriteLine($"Reducing constraints on restart.");
            _constraintReducer.ReduceLearnedConstraints();
        }

        return true;
    }
}
