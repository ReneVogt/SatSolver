namespace Revo.SatSolver;

/// <summary>
/// The operation modes for the <see cref="SatSolverFactory"/>.
/// </summary>
public enum SatSolverMode
{
    /// <summary>
    /// Uses conflict driven clause learning.
    /// </summary>
    CDCL,

    /// <summary>
    /// Pure DPLL without conflict driven clause learning.
    /// </summary>
    DPLL
}
