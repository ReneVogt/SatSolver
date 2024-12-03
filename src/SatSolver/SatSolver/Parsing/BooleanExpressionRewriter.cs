using Revo.SatSolver.Parsing.Expressions;

namespace Revo.SatSolver.Parsing;

public abstract class BooleanExpressionRewriter
{
    protected BooleanExpressionRewriter()
    {
    }

    public virtual BooleanExpression Rewrite(BooleanExpression expression) => expression switch
    {
        BinaryExpression binaryExpression => RewriteBinaryExpression(binaryExpression),
        UnaryExpression unaryExpression => RewriteUnaryExpression(unaryExpression),
        LiteralExpression literalExpression => RewriteLiteralExpression(literalExpression),
        _ => expression
    };

    public virtual BooleanExpression RewriteBinaryExpression(BinaryExpression expression)
    {
        var left = Rewrite(expression.Left);
        var right = Rewrite(expression.Right);

        return left == expression.Left && right == expression.Right
            ? expression
            : new BinaryExpression(left, expression.Operator, right);
    }

    public virtual BooleanExpression RewriteUnaryExpression(UnaryExpression expression) 
    {
        var exp = Rewrite(expression.Expression);

        return exp == expression.Expression
            ? expression
            : new UnaryExpression(expression.Operator, exp);
    }
    public virtual BooleanExpression RewriteLiteralExpression(LiteralExpression expression) => expression;
}
