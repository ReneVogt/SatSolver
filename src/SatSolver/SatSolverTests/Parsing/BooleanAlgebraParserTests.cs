using Revo.SatSolver.Parsing;
using Revo.SatSolver.Parsing.Expressions;
using Xunit.Abstractions;

namespace SatSolverTests.Parsing;
public class BooleanAlgebraParserTests(ITestOutputHelper? output)
{
    ITestOutputHelper? _output = output;

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

    void DumpExpression(BooleanExpression expression, string indent = "")
    {
        switch (expression)
        {
            case BinaryExpression binaryExpression:
                _output?.WriteLine($"{indent}{binaryExpression.Operator}");
                _output?.WriteLine($"{indent} Left:");
                DumpExpression(binaryExpression.Left, indent + "    ");
                _output?.WriteLine($"{indent} Right:");
                DumpExpression(binaryExpression.Right, indent + "    ");
                break;
            case UnaryExpression unaryExpression:
                _output?.WriteLine($"{indent}Not");
                _output?.WriteLine($"{indent} Expression:");
                DumpExpression(unaryExpression.Expression, indent + "    ");
                break;
            case LiteralExpression literalExpression:
                _output?.WriteLine($"{indent}'{literalExpression.Name}");
                break;
        }
    }
}
