using System.Collections.Immutable;

namespace Revo.SatSolver;

/// <summary>
/// Represents a clause in a SATisfiability <see cref="Problem"/>.
/// </summary>
/// <param name="Literals">An array of <see cref="Literal"/>s contained in this clause.</param>
public sealed class Clause
{
    public ImmutableArray<Literal> Literals { get; }
    
    public Clause(IEnumerable<Literal> literals)
    {
        _ = literals ?? throw new ArgumentNullException(nameof(literals));
        Literals = literals.OrderBy(literal => literal.Id).ThenBy(literal => literal.Sense).ToImmutableArray();
    }

    public override int GetHashCode() =>  Literals.Aggregate(17, (hash, value) => hash * 371 + value.GetHashCode());
    public override bool Equals(object? obj) => 
        obj is Clause { Literals: var other } && 
        other.Length == Literals.Length && 
        other.SequenceEqual(Literals);

    public override string ToString() => string.Join(" ", Literals) + " 0";

    public static implicit operator Clause(Literal[] literals) => new(literals);
    public static implicit operator Clause(int[] literals) => new(literals.Select(i => (Literal)i).ToArray());
}

