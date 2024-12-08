using System.Collections.Immutable;

namespace Revo.SatSolver;

/// <summary>
/// Represents a clause in a SATisfiability <see cref="Problem"/>.
/// </summary>
public sealed class Clause : IComparable<Clause>, IEquatable<Clause>
{
    /// <summary>
    /// An array of <see cref="Literal"/>s contained in this clause.
    /// The literals are sorted by their <see cref="Literal.Id"/> and 
    /// then by their <see cref="Literal.Sense"/>.
    /// </summary>
    public ImmutableArray<Literal> Literals { get; }

    /// <summary>
    /// Creates a new <see cref="Clause"/> instance with the
    /// given <paramref name="literals"/>.
    /// </summary>
    /// <param name="literals">A sequence of <see cref="Literal"/>s. These will
    /// be sorted by <see cref="Literal.Id"/> and then <see cref="Literal.Sense"/>. Duplicate literals (same ID and sense) are removed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="literals"/> is <c>null</c>.</exception>    
    public Clause(IEnumerable<Literal> literals)
    {
        _ = literals ?? throw new ArgumentNullException(nameof(literals));
        Literals = literals.Distinct().OrderBy(literal => literal.Id).ThenBy(literal => literal.Sense).ToImmutableArray();
    }

    public override int GetHashCode() =>  Literals.Aggregate(Literals.Length.GetHashCode(), (hash, literal) => hash * 371 + literal.GetHashCode());
    public override bool Equals(object? obj) => obj is Clause other && Equals(other); 
    public bool Equals(Clause? other) => other?.Literals.SequenceEqual(Literals) ?? false;
    public int CompareTo(Clause? other)
    {
        if (other is not { Literals: var literals}) return 1;
        if (Literals.Length < literals.Length) return -1;
        if (Literals.Length > literals.Length) return 1;
        for (var i=0; i<Literals.Length; i++)
        {
            var mine = Literals[i];
            var others = literals[i];
            if (mine.Id < others.Id) return -1;
            if (mine.Id > others.Id) return 1;
            if (!mine.Sense && others.Sense) return -1;
            if (mine.Sense && !others.Sense) return 1;
        }

        return 0;
    }

    public override string ToString() => string.Join(" ", Literals) + (Literals.Length > 0 ? " 0" : "0");

    public static implicit operator Clause(Literal[] literals) => new(literals);
    public static implicit operator Clause(int[] literals) => new(literals.Select(i => (Literal)i).ToArray());

    public static bool operator ==(Clause left, Clause right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(Clause left, Clause right) => left is null ? right is not null : !left.Equals(right);
    public static bool operator <(Clause left, Clause right) => left is null ? right is not null : left.CompareTo(right) < 0;
    public static bool operator <=(Clause left, Clause right) => left is null || left.CompareTo(right) <= 0;
    public static bool operator >(Clause left, Clause right) => left is not null && left.CompareTo(right) > 0;
    public static bool operator >=(Clause left, Clause right) => left is null ? right is null : left.CompareTo(right) >= 0;
}

