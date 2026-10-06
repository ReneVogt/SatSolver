using Revo.SatSolver;

namespace TestHelpers;

public static class SolutionValidator
{
    /// <summary>
    /// Validates a complete satisfying assignment containing exactly one entry for
    /// every ID in 1..NumberOfLiterals, including variables absent from all clauses.
    /// The order of entries does not matter.
    /// </summary>
    public static void Validate(Problem problem, Literal[] solution) => ValidateModel(problem, solution);

    static string ValidateModel(Problem problem, Literal[] solution)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(solution);

        if (solution.Length != problem.NumberOfLiterals)
            throw new ArgumentException($"Expected {problem.NumberOfLiterals} assignments, but received {solution.Length}.", nameof(solution));

        var values = new char[problem.NumberOfLiterals];
        foreach (var literal in solution)
        {
            if (literal is null)
                throw new ArgumentException("Solution contains a null literal.", nameof(solution));
            if (literal.Id < 1 || literal.Id > problem.NumberOfLiterals)
                throw new ArgumentException($"Variable ID {literal.Id} is outside 1..{problem.NumberOfLiterals}.", nameof(solution));
            if (values[literal.Id-1] != '\0')
                throw new ArgumentException($"Variable ID {literal.Id} is assigned more than once.", nameof(solution));

            values[literal.Id-1] = literal.Sense ? '1' : '0';
        }

        // Correct length, valid IDs and uniqueness imply that every variable is assigned.
        foreach (var clause in problem.Clauses)
            if (!clause.Literals.Any(literal => values[literal.Id-1] == (literal.Sense ? '1' : '0')))
                throw new ArgumentException($"Solution does not satisfy clause: {clause}", nameof(solution));

        return new string(values);
    }

    /// <summary>
    /// Validates complete satisfying assignments and rejects duplicate models,
    /// regardless of entry order. Differences in unused variables distinguish models.
    /// This does not establish that all models have been supplied or that an empty
    /// sequence implies unsatisfiability.
    /// </summary>
    public static void Validate(Problem problem, IEnumerable<Literal[]> solutions)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(solutions);

        var models = new HashSet<string>(StringComparer.Ordinal);
        foreach (var solution in solutions)
            if (!models.Add(ValidateModel(problem, solution)))
                throw new ArgumentException("Duplicate model in solution sequence.", nameof(solutions));
    }
}
