namespace Revo.SatSolver.BooleanAlgebra;

static class SyntaxFacts
{
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

    public static BinaryOperator ParseBinaryOperator(char c) => c switch
    {
        '&' => BinaryOperator.And,
        '%' => BinaryOperator.Xor,
        '|' => BinaryOperator.Or,
        _ => BinaryOperator.Unknown
    };
    public static int GetPrecedence(this BinaryOperator op) => op switch
    {
        BinaryOperator.And => 10,
        BinaryOperator.Xor => 5,
        BinaryOperator.Or => 1,
        _ => 0
    };

    const string SpecialCharacters = "()!|&10%";
    public static bool IsSpecialCharacter(this char c) => SpecialCharacters.Contains(c);
}
