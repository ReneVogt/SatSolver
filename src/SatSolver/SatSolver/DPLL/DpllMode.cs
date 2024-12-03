namespace Revo.SatSolver.DPLL;

/// <summary>
/// The processing mode of the <see cref="DpllProcessor"/>.
/// </summary>
enum DpllMode
{
    /// <summary>
    /// All solutions should be enumerated.
    /// This can take longer to find the first solution.
    /// </summary>
    AllSolutions = 0,

    /// <summary>
    /// Optimizes processing to quickly find the first 
    /// solution to decide if the SAT problem is 
    /// satisfiable or not.
    /// </summary>
    DecisionOnly
}
