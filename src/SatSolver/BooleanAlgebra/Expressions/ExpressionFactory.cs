﻿namespace Revo.BooleanAlgebra.Expressions;

public static class ExpressionFactory
{
    public static UnaryExpression Not(BooleanExpression expression) => new(UnaryOperator.Not, expression);
    public static BinaryExpression Or(this BooleanExpression left, BooleanExpression right) => new(left, BinaryOperator.Or, right);
    public static BinaryExpression Xor(this BooleanExpression left, BooleanExpression right) => new(left, BinaryOperator.Xor, right);
    public static BinaryExpression And(this BooleanExpression left, BooleanExpression right) => new(left, BinaryOperator.And, right);
    public static LiteralExpression Literal(string name) => new(name);
    public static ConstantExpression One { get; } = new ConstantExpression(true);
    public static ConstantExpression Zero { get; } = new ConstantExpression(false);
}
