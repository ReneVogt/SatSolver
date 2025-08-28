using Revo.SatSolver.DataStructures;
using System.Runtime.CompilerServices;

namespace Revo.SatSolver.Tools;

sealed class PropagationRateTracker(int fastHalflife, int slowHalflife, double _threshold, int _holdForConflicts, int _coolDownForConflicts) : ITrackPropagationRate
{
    readonly Ema _fastEma = new(fastHalflife);
    readonly Ema _slowEma = new(slowHalflife);

    public double CurrentRatio { get; private set; } = 1;

    int _propagationsSinceLastConflict;
    int _conflictsSinceLastRestart;
    int _conflictsSinceTriggered;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddConflict()
    {
        _fastEma.Push(_propagationsSinceLastConflict);
        _slowEma.Push(_propagationsSinceLastConflict);
        _propagationsSinceLastConflict = 0;

        CurrentRatio = _slowEma.Value != 0 ? _fastEma.Value / _slowEma.Value : 1;

        if (CurrentRatio < _threshold)
            _conflictsSinceTriggered++;
        else
            _conflictsSinceTriggered = 0;

        _conflictsSinceLastRestart++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddPropagation() => _propagationsSinceLastConflict++;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldRestart() => _conflictsSinceLastRestart >= _coolDownForConflicts && _conflictsSinceTriggered >= _holdForConflicts;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetAfterRestart() => _conflictsSinceLastRestart = _conflictsSinceTriggered = 0;    
}
