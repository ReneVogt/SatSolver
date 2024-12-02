using Revo.SatSolver.Parsing.Expressions;

namespace Revo.SatSolver.Parsing;

public sealed class BooleanAlgebraParser
{
    const string KnownCharacters = "()!|&";

    readonly string _input;

    int _position;

    bool EndReached => _position >= _input.Length;
    char Current => !EndReached ? _input[_position] : '\0';

    BooleanAlgebraParser(string input) => _input = input ?? throw new ArgumentNullException(nameof(input));

    BooleanExpression Parse()
    {
        var expression = ParseExpression();
        SkipWhiteSpace();
        if (!EndReached)
            throw InvalidBooleanAlgebraException.InvalidCharacter(_position);
        
        return expression;
    }

    void SkipWhiteSpace()
    {
        while (!EndReached && char.IsWhiteSpace(Current)) _position++;
    }

    BooleanExpression ParseExpression(int parentPrecedence = 0)
    {
        SkipWhiteSpace();

        BooleanExpression left;
        if (Current == '!')
        {
            _position++;
            left = new UnaryExpression(UnaryOperator.Not, ParseExpression(3));
        }
        else
            left = ParsePrimaryExpression();
        

        for (; ; )
        {
            SkipWhiteSpace();
            var current = Current;
            var precedence = current switch
            {
                '&' => 2,
                '|' => 1,
                _ => 0
            };
            if (precedence <= parentPrecedence) return left;

            _position++;
            var right = ParseExpression(precedence);
            left = new BinaryExpression(left, current == '&' ? BinaryOperator.And : BinaryOperator.Or, right);
        }
    }
    BooleanExpression ParsePrimaryExpression()
    {
        SkipWhiteSpace();
        return Current == '(' ? ParseParenthesizedExpression() : ParseLiteralExpression();
    }
    BooleanExpression ParseParenthesizedExpression()
    {
        _position++;
        var expression = ParseExpression();
        SkipWhiteSpace();
        if (EndReached) throw InvalidBooleanAlgebraException.UnexpectedEnd(_position);
        if (Current != ')') throw InvalidBooleanAlgebraException.InvalidCharacter(_position);
        _position++;
        return expression;
    }
    LiteralExpression ParseLiteralExpression()
    {
        var start = _position;
        if (EndReached)
            throw InvalidBooleanAlgebraException.UnexpectedEnd(_position);

        while (!(EndReached || KnownCharacters.Contains(Current) || char.IsWhiteSpace(Current))) _position++;
        if (start == _position)
            throw InvalidBooleanAlgebraException.InvalidCharacter(_position);

        return new LiteralExpression(_input[start.._position]);
    }

    public static BooleanExpression Parse(string input)
    {
        var parser = new BooleanAlgebraParser(input);
        return parser.Parse();
    }
}
