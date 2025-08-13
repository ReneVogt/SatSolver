namespace Revo.SatSolver.DPLL;

sealed class PropagationRateTracker(int conflictInterval, int sampleSize, double decay)
{    
    readonly Queue<double> _recentRates = new(sampleSize+1);
    double _ema;

    int _conflictCount, _propagationCount; 

    public double CurrentRatio { get; private set; } = 1;

    public void AddConflict()
    {
        _conflictCount++;
        if (_conflictCount < conflictInterval) return;

        AddRate(_propagationCount/(double)_conflictCount);
        _conflictCount = _propagationCount = 0;
    }
    public void AddPropagations(int propgations) => _propagationCount += propgations;

    void AddRate(double rate) 
    {
        _recentRates.Enqueue(rate);
        if (_recentRates.Count > sampleSize)
        {
            _recentRates.Dequeue();
            _ema = decay * _ema + (1 - decay) * rate;
            var recentAverage = _recentRates.Average();
            CurrentRatio = recentAverage / _ema;
            return;
        }
        if (_recentRates.Count < sampleSize) return;
        _ema = _recentRates.Average();
    }
}
