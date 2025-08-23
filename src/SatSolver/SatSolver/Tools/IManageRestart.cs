namespace Revo.SatSolver.Tools;

interface IManageRestart
{
    void AddConflict();
    bool RestartIfNecessary();
}