namespace Revo.SatSolver.Tools;

interface ITrackPropagationRate
{
    double CurrentRatio { get; }

    void AddConflict();
    void AddPropagation();
}