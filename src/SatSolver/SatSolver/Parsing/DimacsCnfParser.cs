using System.Globalization;
using static Revo.SatSolver.Parsing.CnfParserException;

namespace Revo.SatSolver.Parsing;

/// <summary>
/// Parses SATisfiability problems written in
/// conjunctive normal form in DIMACS standard.
/// </summary>
public sealed class DimacsCnfParser
{
    readonly List<Problem> _problems = [];
    readonly List<Clause> _clauses = [];
    readonly string _input;

    int _lineNumber, _position, _lineStart;
    uint _numberOfLiterals, _numberOfClauses;

    char Current => EndReached ? '\0' : _input[_position];
    char Next => EndReached || _position+1 >= _input.Length ? '\0' : _input[_position+1];
    bool EndReached => _position >= _input.Length || _input[_position] == '%';
    int Column => _position - _lineStart;

    DimacsCnfParser(string input) => _input = input;

    void Parse()
    {
        do
        {
            MoveToNextLineStart(skipCurrentLine: false);
            ReadProblem();
        } while (!EndReached);
    }
    void ReadProblem()
    {
        ReadProblemLine();
        _clauses.Clear();
        for (var i = 0; i<_numberOfClauses; i++)
            ReadClause();
        _problems.Add(new((int)_numberOfLiterals, [.. _clauses]));
    }
    void ReadProblemLine()
    {
        if (Current != 'p' || Next != ' ') throw InvalidProblemLine(_lineNumber);
        _position++;

        SkipWhiteSpacesOnLine();
        var start = _position;
        while (!EndReached && !char.IsWhiteSpace(Current)) _position++;
        if (start == _position) throw InvalidProblemLine(_lineNumber, start-_lineStart);
        var format = _input[start.._position];
        if (format != "cnf") throw InvalidProblemFormat(format, _lineNumber, start-_lineStart);

        SkipWhiteSpacesOnLine();
        start = _position;
        while (!EndReached && !char.IsWhiteSpace(Current)) _position++;
        if (start == _position) throw InvalidProblemLine(_lineNumber, start-_lineStart);
        var text = _input[start.._position];
        if (!uint.TryParse(text, CultureInfo.InvariantCulture, out _numberOfLiterals) || _numberOfLiterals < 1)
            throw InvalidProblemLine(_lineNumber, start-_lineStart);

        SkipWhiteSpacesOnLine();
        start = _position;
        while (!EndReached && !char.IsWhiteSpace(Current)) _position++;
        if (start == _position) throw InvalidProblemLine(_lineNumber, start-_lineStart);
        text = _input[start.._position];
        if (!uint.TryParse(text, CultureInfo.InvariantCulture, out _numberOfClauses) || _numberOfClauses < 1)
            throw InvalidProblemLine(_lineNumber, start-_lineStart);

        MoveToNextLineStart();
    }
    void ReadClause()
    {
        var literals = new List<Literal>();
        var done = false;
        while (!done)
        {
            var start = Column;
            var literal = ReadLiteral();
            if (literal.Id > _numberOfLiterals) throw LiteralOutOfRange(literal.Id, (int)_numberOfLiterals, _lineNumber, start);

            if (literal.Id != 0)
            {
                literals.Add(literal);
                continue;
            }

            if (literals.Count == 0) throw MissingLiteral(_clauses.Count+1, (int)_numberOfClauses, _lineNumber, start);
            _clauses.Add(new([.. literals]));
            MoveToNextLineStart();
            done = true;
        }
    }
    Literal ReadLiteral()
    {
        var start = _position;
        while (!EndReached && !char.IsWhiteSpace(Current)) _position++;
        if (start == _position) throw MissingLiteral(_clauses.Count+1, (int)_numberOfClauses, _lineNumber, start-_lineStart);
        if (!int.TryParse(_input[start.._position], CultureInfo.InvariantCulture, out var literal))
            throw MissingLiteral(_clauses.Count+1, (int)_numberOfClauses, _lineNumber, start-_lineStart);
        SkipWhiteSpacesOnLine();
        return new(Math.Abs(literal), literal > 0);
    }

    void MoveToNextLineStart(bool skipCurrentLine = true)
    {
        if (skipCurrentLine) SkipLine();
        SkipWhiteSpacesOnLine();

        while (!EndReached && (Current == 'c' || Current == '\n'))
        {
            SkipLine();
            SkipWhiteSpacesOnLine();
        }

        void SkipLine()
        {
            while (!EndReached && Current != '\n') _position++;
            if (EndReached) return;
            _position++;
            _lineNumber++;
            _lineStart = _position;
        }
    }
    void SkipWhiteSpacesOnLine()
    {
        while (!EndReached && char.IsWhiteSpace(Current) && Current != '\n')
            _position++;
    }

    /// <summary>
    /// Parses the <paramref name="input"/> string into a set of SATisfiability
    /// <see cref="Problem"/>s.
    /// </summary>
    /// <param name="input">The input string in DIMACS conjunctive normal form.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">The <paramref name="input"/> string was <c>null</c>.</exception>
    /// <exception cref="CnfParserException">The input string could not be parsed successfully.</exception>
    public static Problem[] Parse(string input)
    {
        var parser = new DimacsCnfParser(input ?? throw new ArgumentNullException(nameof(input)));
        parser.Parse();
        return [.. parser._problems];
    }

}
