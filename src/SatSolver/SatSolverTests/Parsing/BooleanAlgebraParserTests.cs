using Revo.SatSolver.Parsing;
using SatSolverTests.BooleanAlgebra;

namespace SatSolverTests.Parsing;
public class BooleanAlgebraParserTests()
{
    [Fact]
    public void Parse_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BooleanAlgebraParser.Parse(null!));
    }

    [Fact]
    public void Parse_True()
    {
        const string input = "1";
        var expression = BooleanAlgebraParser.Parse(input);
        using var e = new BooleanExpressionAsserter(expression);

        e.AssertConstant(true);
    }
    [Fact]
    public void Parse_False()
    {
        const string input = "0";
        var expression = BooleanAlgebraParser.Parse(input);
        using var e = new BooleanExpressionAsserter(expression);

        e.AssertConstant(false);
    }
    [Fact]
    public void Parse_Literal()
    {
        const string input = "a";
        var expression = BooleanAlgebraParser.Parse(input);
        using var e = new BooleanExpressionAsserter(expression);

        e.AssertLiteral("a");
    }
    [Fact]
    public void Parse_Or()
    {
        const string input = "a | b";
        var expression = BooleanAlgebraParser.Parse(input);
        using var e = new BooleanExpressionAsserter(expression);
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("b");
    }
    [Fact]
    public void Parse_And()
    {
        const string input = "a &\nb";
        var expression = BooleanAlgebraParser.Parse(input);
        using var e = new BooleanExpressionAsserter(expression);
        e.AssertAnd();
        e.AssertLiteral("a");
        e.AssertLiteral("b");
    }
    [Fact]
    public void Parse_Not()
    {
        const string input = "!not";
        var expression = BooleanAlgebraParser.Parse(input);
        using var e = new BooleanExpressionAsserter(expression);
        e.AssertNot();
        e.AssertLiteral("not");
    }
    [Fact]
    public void Parse_DoubleNot()
    {
        const string input = "a | !!b";
        var expression = BooleanAlgebraParser.Parse(input);
        using var e = new BooleanExpressionAsserter(expression);
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertNot();
        e.AssertNot();
        e.AssertLiteral("b");
    }

    [Fact]
    public void Parse_ParenthesizedWithPrecedence()
    {
        const string input = "ab | (c & (a | d)) & !(x | !z) | !t";
        var expression = BooleanAlgebraParser.Parse(input);
        using var e = new BooleanExpressionAsserter(expression);

        e.AssertOr();
        e.AssertOr();
        e.AssertLiteral("ab");

        e.AssertAnd();

        e.AssertAnd();
        e.AssertLiteral("c");
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertLiteral("d");

        e.AssertNot();
        e.AssertOr();
        e.AssertLiteral("x");
        e.AssertNot();
        e.AssertLiteral("z");

        e.AssertNot();
        e.AssertLiteral("t");
    }
    [Fact]
    public void Parse_Complex1()
    {
        const string input = "ab | (0 & (a | 1)) & !(x | !z) | !0";
        var expression = BooleanAlgebraParser.Parse(input);
        using var e = new BooleanExpressionAsserter(expression);

        e.AssertOr();
        e.AssertOr();
        e.AssertLiteral("ab");

        e.AssertAnd();

        e.AssertAnd();
        e.AssertConstant(false);
        e.AssertOr();
        e.AssertLiteral("a");
        e.AssertConstant(true);

        e.AssertNot();
        e.AssertOr();
        e.AssertLiteral("x");
        e.AssertNot();
        e.AssertLiteral("z");

        e.AssertNot();
        e.AssertConstant(false);
    }

    [
        Theory,
        InlineData("", 0, InvalidBooleanAlgebraException.Reason.UnexpectedEnd),
        InlineData("a b", 2, InvalidBooleanAlgebraException.Reason.InvalidOrUnexpectedCharacter),
        InlineData("(a | b) && c", 9, InvalidBooleanAlgebraException.Reason.InvalidOrUnexpectedCharacter),
        InlineData("a | (b | c & d", 14, InvalidBooleanAlgebraException.Reason.UnexpectedEnd),
        InlineData("a! c", 1, InvalidBooleanAlgebraException.Reason.InvalidOrUnexpectedCharacter),
        InlineData("a & (b | c))", 11, InvalidBooleanAlgebraException.Reason.InvalidOrUnexpectedCharacter),
        InlineData("(a & b!", 6, InvalidBooleanAlgebraException.Reason.InvalidOrUnexpectedCharacter),
        InlineData("(a0 & b)", 2, InvalidBooleanAlgebraException.Reason.InvalidOrUnexpectedCharacter),
        InlineData("(0a & b)", 2, InvalidBooleanAlgebraException.Reason.InvalidOrUnexpectedCharacter),
    ]
    public void Parse_InvalidSyntax_Exception(string input, int position, InvalidBooleanAlgebraException.Reason reason)
    {
        var exception = Assert.Throws<InvalidBooleanAlgebraException>(() => BooleanAlgebraParser.Parse(input));
        Assert.Equal(position, exception.Position);
        Assert.Equal(reason, exception.Error);
    }
}
