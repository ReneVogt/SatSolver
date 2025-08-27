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

    readonly bool _useRestarts;
    readonly double _propagationRateThreshold, _literalBlockDistanceThreshold;
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
        double? propagationRateThreshold, double? literalBlockDistanceThreshold,
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
        _literalBlockDistanceThreshold = literalBlockDistanceThreshold ?? double.MaxValue;
        _propagationRateThreshold = propagationRateThreshold ?? 0d;

        _useRestarts = restartInterval is not null  ||
            lubySequence is not null ||
            propagationRateThreshold is not null ||
            literalBlockDistanceThreshold is not null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddConflict() => _restartCounter++;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RestartIfNecessary()
    {
        if (!_useRestarts) return false;

        var propagationRateRatio = _propagationRateTracker.CurrentRatio;
        var literalBlockDistanceRatio = _literalBlockDistanceTracker.CurrentRatio;

        if (!(_restartCounter > _nextRestartThreshold || propagationRateRatio < _propagationRateThreshold || literalBlockDistanceRatio > _literalBlockDistanceThreshold)) return false;

        Debug.WriteLine($"Restarting (counter: {_restartCounter} / {_nextRestartThreshold}, propagation rate: {propagationRateRatio} / {_propagationRateThreshold}, lbd: {literalBlockDistanceRatio} / {_literalBlockDistanceThreshold}).");

        _restartCounter = 0;
        if (_lubySequence is not null)
            _nextRestartThreshold = _lubySequence.Next();

        _trail.Reset();
        _unitPropagationQueue.Clear();

        if (_reduceConstraints)
        {
            Debug.WriteLine($"Reducing constraints on restart.");
            _constraintReducer.ReduceLearnedConstraints();
        }

        return true;
    }
}
