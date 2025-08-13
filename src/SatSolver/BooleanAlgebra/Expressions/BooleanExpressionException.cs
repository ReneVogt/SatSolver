using System.Diagnostics.CodeAnalysis;

namespace Revo.BooleanAlgebra.Expressions;

[ExcludeFromCodeCoverage]
static class BooleanExpressionException
{
    public static NotSupportedException UnsupportedExpressionKind(ExpressionKind kind) => new($"Unsupported expression kind '{kind}'.");
    public static NotSupportedException UnsupportedUnaryOperator(UnaryOperator op) => new($"Unsupported unary operator '{op}'.");
    public static NotSupportedException UnsupportedBinaryOperator(BinaryOperator op) => new($"Unsupported binary operator '{op}'.");
}
