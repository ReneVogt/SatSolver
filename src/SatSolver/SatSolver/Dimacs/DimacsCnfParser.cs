using System.Globalization;
using static Revo.SatSolver.Dimacs.CnfParserException;

namespace Revo.SatSolver.Dimacs;

/// <summary>
/// Parses satisfiability problems written in
/// conjunctive normal form in DIMACS standard.
/// </summary>
public sealed class DimacsCnfParser
{
    readonly List<Problem> _problems = [];
    readonly List<Clause> _clauses = [];
    readonly List<Literal> _literals = [];
    readonly string _input;

    int _lineNumber, _position, _lineStart;
    int _numberOfLiterals, _numberOfClauses;

    char Current => _position < _input.Length ? _input[_position] : '\0';
    bool EndReached => _position >= _input.Length;
    int Column => _position - _lineStart;

    DimacsCnfParser(string input) => _input = input;

    void Parse()
    {
        do
        {
            ReadProblem();
            SkipTrivialLines();
        } while (_position < _input.Length);
    }
    void ReadProblem()
    {
        ReadProblemLine();
        for (var i = 0; i<_numberOfClauses; i++)
            ReadClause();
        _problems.Add(new(_numberOfLiterals, [.. _clauses]));
        _clauses.Clear();
    }
    void ReadProblemLine()
    {
        SkipTrivialLines();
        if (EndReached || Current != 'p') throw InvalidProblemLine(_lineNumber);
        _position++;
        if (Current != ' ') throw InvalidProblemLine(_lineNumber);
        _position++;
        var format = ReadText();
        if (format != "cnf") throw InvalidProblemFormat(format, _lineNumber);
        if (Current != ' ') throw InvalidProblemLine(_lineNumber, Column);
        _position++;
        _numberOfLiterals = ReadNumber();
        if (_numberOfLiterals < 1) throw InvalidProblemLine(_lineNumber, Column);
        if (Current != ' ') throw InvalidProblemLine(_lineNumber, Column);
        _position++;
        _numberOfClauses = ReadNumber();
        if (_numberOfClauses < 1) throw InvalidProblemLine(_lineNumber, Column);
        FinishLine();
    }
    void ReadClause()
    {
        SkipTrivialLines();
        if (EndReached || Current == 'p') throw MissingClauses(_numberOfClauses - _clauses.Count, _lineNumber);

        for (; ; )
        {
            var literal = ReadLiteral();
            if (literal.Id == 0)
            {
                if (_literals.Count == 0) throw MissingLiteral(_lineNumber, Column);
                _clauses.Add(new([.. _literals]));
                FinishLine();
                return;
            }
            if (literal.Id > _numberOfLiterals) throw LiteralOutOfRange(literal.Id, _numberOfLiterals, _lineNumber, Column);
            _literals.Add(literal);
        }
    }
    Literal ReadLiteral()
    {
        while (!EndReached && char.IsWhiteSpace(Current) && Current != '\n') _position++;
        if (EndReached || Current == '\n') throw MissingTerminator(_lineNumber, Column);

        var text = ReadText();
        if (!int.TryParse(text, CultureInfo.InvariantCulture, out var id)) throw InvalidCharacter(text, _lineNumber, Column-text.Length);
        return new(Math.Abs(id), id > 0);
    }
    string ReadText()
    {
        var start = _position;
        while (!EndReached && !char.IsWhiteSpace(Current)) _position++;
        return _input[start.._position];
    }
    int ReadNumber()
    {
        var text = ReadText();
        if (string.IsNullOrEmpty(text)) return -1;
        if (!uint.TryParse(text, CultureInfo.InvariantCulture, out var number))
            throw InvalidCharacter(text, _lineNumber, Column-text.Length);
        return (int)number;
    }
    void SkipTrivialLines()
    {
        while (!EndReached && (Current == 'c' || char.IsWhiteSpace(Current)))
        {
            var comment = Current == 'c';
            while (!EndReached && Current != '\n')
            {
                if (!comment && !char.IsWhiteSpace(Current))
                    throw InvalidLine(_lineNumber);
                _position++;
            }
            if (Current == '\n')
            {
                _position++;
                _lineNumber++;
                _lineStart = _position;
            }
        }
    }
    void FinishLine()
    {
        while (!EndReached && Current != '\n')
        {
            if (!char.IsWhiteSpace(Current))
                throw InvalidCharacter(Current.ToString(), _lineNumber, Column);
            _position++;
        }
        _position++;
        _lineNumber++;
        _lineStart = _position;
    }

    /// <summary>
    /// Parses the <paramref name="input"/> string into a set of satisfiability
    /// <see cref="Problem"/> problems.
    /// </summary>
    /// <param name="input">The input string in DIMACS conjunctive normal form.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">The <paramref name="input"/> string was <c>null</c>.</exception>
    public static Problem[] Parse(string input)
    {
        var parser = new DimacsCnfParser(input ?? throw new ArgumentNullException(nameof(input)));
        parser.Parse();
        return [.. parser._problems];
    }

}
