namespace Revo.SatSolver.Tools;

interface ITrackLiteralBlockDistance
{
    double Average { get; }
    double CurrentRatio { get; }
    bool ShouldRestart();
    void ResetAfterRestart();
    void AddLiteralBlockDistance(int literalBlockDistance);
}