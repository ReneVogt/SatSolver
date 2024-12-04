namespace Revo.SatSolver;

/// <summary>
/// Represents a literal in a SATisfiability <see cref="Problem"/>.
/// </summary>
/// <param name="Id">The id (or index) of this literal. Literal IDs are 1-indexed.</param>
/// <param name="Sense">The sense (or value) of this literal.</param>
public sealed record Literal(int Id, bool Sense)
{
    public override int GetHashCode() => (Sense ? Id : -Id).GetHashCode();
    public override string ToString() => $"{(Sense ? Id : -Id)}";

    public static implicit operator Literal(int id) => new(Math.Abs(id), id > 0);
}

