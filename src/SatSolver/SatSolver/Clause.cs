namespace Revo.SatSolver;

public sealed record Clause(Literal[] Literals)
{
    public static implicit operator Clause(Literal[] literals) => new(literals);
    public static implicit operator Clause(int[] literals) => new(literals.Select(i => (Literal)i).ToArray());
}

