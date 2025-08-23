using Revo.SatSolver.Properties;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Revo.SatSolver;


/// <summary>
/// Represents a SATisfiability problem with literals and clauses.
/// </summary>
public sealed class Problem : IEquatable<Problem>
{
    int? _hash;

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
        if (numberOfLiterals < 0)
            throw new ArgumentException(message: Resources.ProblemArgumentException_NumberOfLiterals, paramName: nameof(numberOfLiterals));
        if (clauses.SelectMany(clause => clause.Literals.Select(literal => literal.Id)).Any(id => id > numberOfLiterals))
            throw new ArgumentException(Resources.ProblemrArgumentException_InvalidLiterals, nameof(clauses));

        NumberOfLiterals = numberOfLiterals;
        Clauses = [.. clauses.OrderBy(clause => clause)];
    }

    [ExcludeFromCodeCoverage]
    public override int GetHashCode()
    {
        if (_hash.HasValue) return _hash.Value;

        var hc = new HashCode();
        hc.Add(NumberOfLiterals);
        foreach (var clause in Clauses) hc.Add(clause);
        _hash = hc.ToHashCode();
        return _hash.Value;
    }

    public override bool Equals(object? obj) => obj is Problem other && Equals(other);
    public bool Equals(Problem? other) => 
        other is not null && 
        other.NumberOfLiterals == NumberOfLiterals && 
        other.NumberOfClauses == NumberOfClauses && 
        other.Clauses.SequenceEqual(Clauses);

    public override string ToString() => $"p cnf {NumberOfLiterals} {NumberOfClauses}{Environment.NewLine}" + string.Join(Environment.NewLine, Clauses);
}

