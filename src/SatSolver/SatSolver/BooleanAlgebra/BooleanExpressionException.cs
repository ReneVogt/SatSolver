namespace Revo.SatSolver.BooleanAlgebra;
static class BooleanExpressionException
{
    public static NotSupportedException UnsupportedExpressionKind(ExpressionKind kind) => new($"Unsupported expression kind '{kind}'.");
    public static NotSupportedException UnsupportedUnaryOperator(UnaryOperator op) => new($"Unsupported unary operator '{op}'.");
    public static NotSupportedException UnsupportedBinaryOperator(BinaryOperator op) => new($"Unsupported binary operator '{op}'.");
}
