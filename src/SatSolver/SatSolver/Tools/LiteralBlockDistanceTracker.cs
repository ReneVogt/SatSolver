using System.Runtime.CompilerServices;

namespace Revo.SatSolver.Tools;
sealed class LiteralBlockDistanceTracker(int size, double decay) : ITrackLiteralBlockDistance
{
    readonly Queue<int> _recent = new(size+1);
    double _ema;

    public double CurrentRatio { get; private set; } = 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddValue(int value)
    {
        _recent.Enqueue(value);
        if (_recent.Count > size)
        {
            _recent.Dequeue();
            _ema = decay * _ema + (1 - decay) * value;
            var recentAverage = _recent.Average();
            CurrentRatio = recentAverage / _ema;
            return;
        }
        if (_recent.Count < size) return;
        _ema = _recent.Average();
    }
}
