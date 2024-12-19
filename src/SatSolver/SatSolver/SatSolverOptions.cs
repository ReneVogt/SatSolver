namespace Revo.SatSolver;
public record SatSolverOptions(bool UnitPropagation = true, bool PureLiteralElimination = false, bool RemoveSupersets = true)
{
    public static SatSolverOptions AllSolutions { get; } = new(UnitPropagation: true, PureLiteralElimination: false, RemoveSupersets: true);
    public static SatSolverOptions Satisfiability { get; } = new(UnitPropagation: true, PureLiteralElimination: true, RemoveSupersets: true);
}
