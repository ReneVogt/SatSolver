using Revo.SatSolver.DataStructures;
using System.Diagnostics;

namespace Revo.SatSolver.Tools;

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

    public RestartManager(SatSolverState state)
    {
        _trail = state.VariableTrail;
        _propagationRateTracker = state.PropagationRateTracker;
        _literalBlockDistanceTracker = state.LiteralBlockDistanceTracker;
        _unitLiterals = state.UnitsToPropagate;
        var options = state.Options;

        if (options.Restart.Interval is { } restartInterval)
            if (options.Restart.Luby)
            {
                _lubySequence = new LubySequence(restartInterval);
                _nextRestartThreshold = (int)_lubySequence.Next();
            }
            else
                _nextRestartThreshold = restartInterval;

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
