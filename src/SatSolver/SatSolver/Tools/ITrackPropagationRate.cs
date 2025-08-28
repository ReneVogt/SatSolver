namespace Revo.SatSolver.Tools;

interface ITrackPropagationRate
{
    double CurrentRatio { get; }
    bool ShouldRestart();
    void ResetAfterRestart();
    void AddPropagation();
    void AddConflict();
}