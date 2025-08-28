namespace Revo.SatSolver.DataStructures;

sealed class Ema(double halfLife)
{
    public double Value { get; private set; }
    public bool HasValue { get; private set; }
    
    readonly double alpha = 1.0 - Math.Pow(2.0, -1.0 / halfLife);
    public void Push(double x)
    {
        if (HasValue)
            Value = alpha * x + (1 - alpha) * Value;
        else
        {
            Value = x;
            HasValue = true;
        }
    }
}