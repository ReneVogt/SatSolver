namespace Revo.SatSolver;

/// <summary>
/// Represents a clause in a SATisfiability <see cref="Problem"/>.
/// </summary>
/// <param name="Literals">An array of <see cref="Literal"/>s contained in this clause.</param>
public sealed record Clause(Literal[] Literals)
{
    public static implicit operator Clause(Literal[] literals) => new(literals);
    public static implicit operator Clause(int[] literals) => new(literals.Select(i => (Literal)i).ToArray());
}

