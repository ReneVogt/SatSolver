namespace Revo.SatSolver.Tools;

interface ITrackLiteralBlockDistance
{
    double CurrentRatio { get; }

    void AddValue(int value);
}