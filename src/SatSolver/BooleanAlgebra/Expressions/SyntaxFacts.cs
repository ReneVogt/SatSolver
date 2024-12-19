using static Revo.BooleanAlgebra.Expressions.BooleanExpressionException;

namespace Revo.BooleanAlgebra.Expressions;

static class SyntaxFacts
{
    public static char GetOperatorChar(this UnaryOperator op) => op switch
    {
        UnaryOperator.Not => '!',
        _ => throw UnsupportedUnaryOperator(op)
    };
    public static UnaryOperator ParseUnaryOperator(char c) => c switch
    {
        '!' => UnaryOperator.Not,
        _ => UnaryOperator.Unknown
    };
    public static int GetPrecedence(this UnaryOperator op) => op switch
    {
        UnaryOperator.Not => 100,
        _ => 0
    };


    public static char GetOperatorChar(this BinaryOperator op) => op switch
    {
        BinaryOperator.And => '&',
        BinaryOperator.Xor => '%',
        BinaryOperator.Or => '|',
        BinaryOperator.Implication => '>',
        BinaryOperator.ReverseImplication => '<',
        BinaryOperator.Equivalence => '=',
        _ => throw UnsupportedBinaryOperator(op)
    };
    public static BinaryOperator ParseBinaryOperator(char c) => c switch
    {
        '&' => BinaryOperator.And,
        '%' => BinaryOperator.Xor,
        '|' => BinaryOperator.Or,
        '>' => BinaryOperator.Implication,
        '<' => BinaryOperator.ReverseImplication,
        '=' => BinaryOperator.Equivalence,
        _ => BinaryOperator.Unknown
    };
    public static int GetPrecedence(this BinaryOperator op) => op switch
    {
        BinaryOperator.And => 15,
        BinaryOperator.Xor => 10,
        BinaryOperator.Or => 5,
        BinaryOperator.Implication or BinaryOperator.ReverseImplication => 2,
        BinaryOperator.Equivalence => 1,
        _ => 0
    };
}
