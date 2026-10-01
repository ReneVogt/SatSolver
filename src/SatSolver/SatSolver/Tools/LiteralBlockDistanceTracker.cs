using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.Tools;

sealed class LiteralBlockDistanceTracker(int fastHalflife, int slowHalflife, double _threshold, int _holdForConflicts, int _coolDownForConflicts) : ITrackLiteralBlockDistance
{
    readonly Ema _fastEma = new(fastHalflife);
    readonly Ema _slowEma = new(slowHalflife);

    public double Average => _slowEma.Value;
    public double CurrentRatio { get; private set; } = 1;

    int _conflictsSinceLastRestart;
    int _conflictsSinceTriggered;

    public void AddLiteralBlockDistance(int literalBlockDistance)
    {
        _fastEma.Push(literalBlockDistance);
        _slowEma.Push(literalBlockDistance);

        CurrentRatio = _slowEma.Value != 0 ? _fastEma.Value / _slowEma.Value : 1;

        if (CurrentRatio > _threshold)
            _conflictsSinceTriggered++;
        else
            _conflictsSinceTriggered = 0;

        _conflictsSinceLastRestart++;
    }

    public bool ShouldRestart() => _conflictsSinceLastRestart >= _coolDownForConflicts && _conflictsSinceTriggered >= _holdForConflicts;

    public void ResetAfterRestart() => _conflictsSinceLastRestart = _conflictsSinceTriggered = 0;
}
