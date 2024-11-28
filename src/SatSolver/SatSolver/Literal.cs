namespace Revo.SatSolver;

public sealed record Literal(int Id, bool Sense)
{
    public static implicit operator Literal(int id) => new(Math.Abs(id), id > 0);
}

