namespace Revo.SatSolver.Tools;

interface ITrackLiteralBlockDistance
{
    double CurrentRatio { get; }
    bool ShouldRestart();
    void ResetAfterRestart();
    void AddLiteralBlockDistance(int literalBlockDistance);
}