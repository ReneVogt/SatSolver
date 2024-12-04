using System.Collections.Immutable;

namespace Revo.SatSolver;


/// <summary>
/// Represents a SATisfiability problem with literals and clauses.
/// </summary>
public sealed class Problem : IEquatable<Problem>
{
    /// <summary>
    /// The number of literals required by the problem.
    /// </summary>
    public int NumberOfLiterals { get; }

    /// <summary>
    /// The number of clauses in this problem.
    /// </summary>
    public int NumberOfClauses => Clauses.Length;

    /// <summary>
    /// The clauses in this problem, sorted by their length, literal IDs and sense.
    /// </summary>
    public ImmutableArray<Clause> Clauses { get; }

    /// <summary>
    /// Creates a new SAT-<see cref="Problem"/>.
    /// </summary>
    /// <param name="numberOfLiterals">The number of <see cref="Literal"/>s in this problem.</param>
    /// <param name="clauses">The clauses this problem contains. They will be sorted by length and literals.</param>
    /// <exception cref="ArgumentNullException"><paramref name="clauses"/> was <c>null</c>.</exception>
    public Problem(int numberOfLiterals, IEnumerable<Clause> clauses)
    {
        _ = clauses ?? throw new ArgumentNullException(nameof(clauses));
        NumberOfLiterals = numberOfLiterals;
        Clauses = clauses.OrderBy(clause => clause).ToImmutableArray();
    }

    public override int GetHashCode() => Clauses.Aggregate(NumberOfLiterals.GetHashCode(), (hash, clause) => hash * 371 + clause.GetHashCode());
    public override bool Equals(object? obj) => obj is Problem other && Equals(other);
    public bool Equals(Problem? other) => 
        other is not null && 
        other.NumberOfLiterals == NumberOfLiterals && 
        other.NumberOfClauses == NumberOfClauses && 
        other.Clauses.SequenceEqual(Clauses);

    public override string ToString() => $"p cnf {NumberOfLiterals} {NumberOfClauses}{Environment.NewLine}" + string.Join(Environment.NewLine, Clauses);
}

