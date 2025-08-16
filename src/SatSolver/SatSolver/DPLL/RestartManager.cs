using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Helpers;
using System.Diagnostics;

namespace Revo.SatSolver.DPLL;

sealed class RestartManager
{
    readonly IVariableTrail _trail;
    readonly PropagationRateTracker _propagationRateTracker;
    readonly EmaTracker _literalBlockDistanceTracker;
    readonly Queue<(ConstraintLiteral, Constraint Reason)> _unitLiterals;
    readonly LubySequence? _lubySequence;

    readonly bool _useRestarts;
    readonly double _propagationRateThreshold, _literalBlockDistanceThreshold;

    int _restartCounter, _nextRestartThreshold;

    public RestartManager(SatSolver.Options options, IVariableTrail trail, PropagationRateTracker propagationRateTracker, EmaTracker literalBlockDistanceTracker, Queue<(ConstraintLiteral, Constraint Reason)> unitLiterals)
    {
        _trail = trail;
        _propagationRateTracker = propagationRateTracker;
        _literalBlockDistanceTracker = literalBlockDistanceTracker;
        _unitLiterals = unitLiterals;

        if (options.Restart.Interval is { } restartInterval)
        {
            if (options.Restart.Luby)
            {
                _lubySequence = new LubySequence(restartInterval);
                _nextRestartThreshold = (int)_lubySequence.Next();
            }
            else
                _nextRestartThreshold = restartInterval;
        }

        _useRestarts = options.Restart.Interval is not null || options.Restart.LiteralBlockDistanceThreshold is not null || options.Restart.PropagationRateThreshold is not null;
        _propagationRateThreshold = options.Restart.PropagationRateThreshold ?? 0;
        _literalBlockDistanceThreshold = options.Restart.LiteralBlockDistanceThreshold ?? double.MaxValue;
    }

    public void AddConflict() => _restartCounter++;
    public bool RestartIfNecessary()
    {
        if (!_useRestarts) return false;

        var restart = _nextRestartThreshold > 0 && _restartCounter > _nextRestartThreshold;
        restart |= _propagationRateTracker.CurrentRatio < _propagationRateThreshold;
        restart |= _literalBlockDistanceTracker.CurrentRatio > _literalBlockDistanceThreshold;

        if (!restart) return false;

        Debug.WriteLine($"Restarting (counter: {_restartCounter} / {_nextRestartThreshold}, propagation rate: {_propagationRateTracker.CurrentRatio} / {_propagationRateThreshold}, lbd: {_literalBlockDistanceTracker.CurrentRatio} / {_literalBlockDistanceThreshold}).");

        _restartCounter = 0;
        if (_lubySequence is not null)
        {
            var next = _lubySequence.Next();
            _nextRestartThreshold = next < int.MaxValue ? (int)next : 0;
        }
        _trail.Reset();
        _unitLiterals.Clear();

        return true;
    }
}
