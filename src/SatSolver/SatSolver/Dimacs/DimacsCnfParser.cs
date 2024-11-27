using System.Globalization;
using static Revo.SatSolver.Dimacs.CnfParserException;

namespace Revo.SatSolver.Dimacs;

public sealed class DimacsCnfParser
{
    public static IEnumerable<Problem> Parse(string input) => ParseInternal(input ?? throw new ArgumentNullException(nameof(input)));

    static IEnumerable<Problem> ParseInternal(string input)
    {
        var lines = input.Split('\n').Select((line, number) => (number, parts: line.Split().Select(part => part.Trim()).Where(part => !string.IsNullOrWhiteSpace(part)).ToArray()));
        var relevantLines = lines.Where(entry => entry.parts.Length > 0 && !entry.parts[0].StartsWith('c'));

        uint numberOfLiterals = 0, numberOfClauses = 0;
        var clauses = new List<Clause>();
        var maxLineNumber = 0;
        foreach (var (lineNumber, parts) in relevantLines)
        {
            if (numberOfLiterals == 0)
            {
                if (parts[0] != "p" || parts.Length != 4)
                    throw MissingProblemLine(lineNumber);
                if (parts[1] != "cnf")
                    throw InvalidProblemFormat(parts[1], lineNumber);
                if (!uint.TryParse(parts[2], CultureInfo.InvariantCulture, out numberOfLiterals) || numberOfLiterals == 0)
                    throw InvalidCharacter(parts[2], lineNumber, 2);
                if (!uint.TryParse(parts[3], CultureInfo.InvariantCulture, out numberOfClauses) || numberOfClauses == 0)
                    throw InvalidCharacter(parts[3], lineNumber, 3);

                continue;
            }

            var literals = new List<Literal>();
            for (var part = 0; part < parts.Length; part++)
            {
                var last = part == parts.Length - 1;
                if (!int.TryParse(parts[part], CultureInfo.InvariantCulture, out var literalId))
                    throw InvalidCharacter(parts[3], lineNumber, part);
                if ((!last && literalId == 0) || literalId > numberOfLiterals)
                    throw LiteralOutOfRange(literalId, (int)numberOfLiterals, lineNumber, part);
                if (last && literalId != 0)
                    throw MissingTerminator(lineNumber, part);

                if (!last) literals.Add(new(Math.Abs(literalId), literalId > 0));
            }
            if (literals.Count < 1)
                throw MissingLiteral(lineNumber, 0);

            clauses.Add(new([.. literals]));
            if (clauses.Count == numberOfClauses)
            {
                yield return new((int)numberOfLiterals, [.. clauses]);
                numberOfLiterals = numberOfClauses = 0;
                clauses.Clear();
            }

            maxLineNumber = lineNumber;
        }

        if (numberOfClauses > 0)
            throw MissingClauses((int)numberOfClauses - clauses.Count, maxLineNumber+1);
    }
}
