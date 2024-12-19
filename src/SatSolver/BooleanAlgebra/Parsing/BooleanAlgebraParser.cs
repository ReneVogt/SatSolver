using Revo.BooleanAlgebra.Expressions;
using static Revo.BooleanAlgebra.Expressions.ExpressionFactory;
using static Revo.BooleanAlgebra.Expressions.BooleanExpressionException;

namespace Revo.BooleanAlgebra.Parsing;

/// <summary>
/// Parses an input string containing a boolean expression
/// into a tree of <see cref="BooleanExpression"/>s.
/// </summary>
public sealed class BooleanAlgebraParser
{
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
        var unaryPrecedence = SyntaxFacts.ParseUnaryOperator(Current).GetPrecedence();
        if (unaryPrecedence > 0 && unaryPrecedence >= parentPrecedence)
        {
            // NOT (!) is currently the only known unary operator.
            _position++;
            left = Not(ParseExpression(unaryPrecedence));
        }
        else
            left = ParsePrimaryExpression();


        for (; ; )
        {
            SkipWhiteSpace();
            var binaryOperator = SyntaxFacts.ParseBinaryOperator(Current);
            var precedence = binaryOperator.GetPrecedence();
            if (precedence <= parentPrecedence) return left;

            _position++;
            var right = ParseExpression(precedence);
            left = binaryOperator switch
            {
                BinaryOperator.And => left.And(right),
                BinaryOperator.Xor => left.Xor(right),
                BinaryOperator.Or => left.Or(right),
                BinaryOperator.Implication => left.Implies(right),
                BinaryOperator.ReverseImplication => left.ImpliedBy(right),
                BinaryOperator.Equivalence => left.Equal(right),
                _ => throw UnsupportedBinaryOperator(binaryOperator)
            };
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
    BooleanExpression ParseLiteralExpression()
    {
        var start = _position;
        if (EndReached)
            throw InvalidBooleanAlgebraException.UnexpectedEnd(_position);

        if (Current == '1')
        {
            _position++;
            return One;
        }
        if (Current == '0')
        {
            _position++;
            return Zero;
        }

        while (!(EndReached || !char.IsLetter(Current) || char.IsWhiteSpace(Current))) _position++;
        if (start == _position)
            throw InvalidBooleanAlgebraException.InvalidCharacter(_position);

        return Literal(_input[start.._position]);
    }

    /// <summary>
    /// Parses the boolean expression in the <paramref name="input"/> string into
    /// a tree of <see cref="BooleanExpression"/>s.
    /// </summary>
    /// <param name="input">The input string containing the boolean expression. Operators like '&' (AND), '|' (OR) and
    /// '!' (NOT) can be used as well as parantheses.</param>
    /// <returns>The root <see cref="BooleanExpression"/> or the expression tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidBooleanAlgebraException">The expression in the <paramref name="input"/> string is invalid.</exception>
    public static BooleanExpression Parse(string input)
    {
        var parser = new BooleanAlgebraParser(input);
        return parser.Parse();
    }
}
