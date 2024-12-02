using Revo.SatSolver.Parsing.Expressions;

namespace Revo.SatSolver.Parsing;

public sealed class BooleanAlgebraParser
{
    readonly string _input;

    int _position;

    bool EndReached => _position >= _input.Length;
    char Current => !EndReached ? _input[_position] : '\0';

    BooleanAlgebraParser(string input) => _input = input ?? throw new ArgumentNullException(nameof(input));

    BooleanExpression Parse() => ParseExpression();

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
        if (Current != ')') throw new NotImplementedException();
        _position++;
        return expression;
    }
    LiteralExpression ParseLiteralExpression()
    {
        var start = _position;
        while (!EndReached && !(Current == '(' || Current == ')' || Current == '&' || Current == '|' || char.IsWhiteSpace(Current))) _position++;
        return new LiteralExpression(_input[start.._position]);
    }

    public static BooleanExpression Parse(string input)
    {
        var parser = new BooleanAlgebraParser(input);
        return parser.Parse();
    }
}
