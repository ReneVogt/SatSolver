using Revo.BooleanAlgebra.Expressions;
using Revo.BooleanAlgebra.Transformers;
using static Revo.BooleanAlgebra.Expressions.ExpressionFactory;

namespace Revo.SatSolver.Parsing;

/// <summary>
/// This transformer takes a <see cref="BooleanExpression"/> and converts
/// it into a conjunctive normal form using Tseitin transformation.
/// And while we're at it, we reduce redundancies like tautologies,
/// contradictions and redundant literals.
/// </summary>
public class TseitinTransformer : BooleanExpressionRewriter
{
    readonly List<(HashSet<string> positives, HashSet<string> negatives)> _clauses = [];
    
    int _tseitinCount;

    LiteralExpression GetNextTseitin() => Literal($".t{_tseitinCount++}");

    protected TseitinTransformer()
    {
    }

    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));

        //
        // If this NOT simply negates a constant, we can
        // return the negated constant.
        //
        if (expression.Expression.Kind == ExpressionKind.Constant && expression.Expression is ConstantExpression { Sense: var sense })
            return sense ? Zero : One;
        //
        // If this NOT simply negates a literal, we can
        // return the expression unchanged.
        //
        if (expression.Expression.Kind == ExpressionKind.Literal)
            return expression;

        //
        // If this is a double negation it can be skipped
        // and the inner expression should be rewritten and
        // returned.
        //
        if (expression.Expression.Kind == ExpressionKind.Unary && expression.Expression is UnaryExpression { Expression: var doubleNegatedExpression })
            return Rewrite(doubleNegatedExpression);

        //
        // So we have a binary expression and rewrite it according to 
        // De Morgan's laws:
        // !(a | b) = !a & !b
        // !(a & b) = !a | !b
        // and the resulting expression is rewritten again.
        //
        var binary = (BinaryExpression)expression.Expression;
        return Rewrite(binary.Operator == BinaryOperator.And ? Not(binary.Left).Or(Not(binary.Right)) : Not(binary.Left).And(Not(binary.Right)));
    }
    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));

        var left = Rewrite(expression.Left);
        var right = Rewrite(expression.Right);

        if (left.Kind == ExpressionKind.Constant && left is ConstantExpression { Sense: var leftConstant })
            return expression.Operator == BinaryOperator.And
                ? leftConstant ? right : Zero
                : leftConstant ? One : right;
        if (right.Kind == ExpressionKind.Constant && right is ConstantExpression { Sense: var rightConstant })
            return expression.Operator == BinaryOperator.And
                ? rightConstant ? left : Zero
                : rightConstant ? One : left;

        //
        // We have rewritten both sides and they are no constants.
        // So they have to be literals or negated literals (either
        // originals or generated Tseitin literals).
        // We now add the appropriate clauses and return a new
        // Tseitin literal.
        //

        var leftSense = left.Kind == ExpressionKind.Literal;
        var leftName = leftSense ? ((LiteralExpression)left).Name : ((LiteralExpression)((UnaryExpression)left).Expression).Name;
        var rightSense = right.Kind == ExpressionKind.Literal;
        var rightName = rightSense ? ((LiteralExpression)right).Name : ((LiteralExpression)((UnaryExpression)right).Expression).Name;
        if (leftName == rightName)
            return expression.Operator == BinaryOperator.Or
                ? leftSense == rightSense ? left : One
                : leftSense == rightSense ? left : Zero;

        var tseitin = GetNextTseitin();

        //
        // We use De Morgan's law for negations, so our
        // polarity is always positive here. Hence, a
        // simple implication is enough.
        // 

        if (expression.Operator == BinaryOperator.Or)
        {
            // x -> (a | b)  ==> !x | a | b
            var positives = new HashSet<string>();
            var negatives = new HashSet<string> { tseitin.Name };
            if (leftSense) positives.Add(leftName); else negatives.Add(leftName);
            if (rightSense) positives.Add(rightName); else negatives.Add(rightName);
            _clauses.Add((positives, negatives));
        }
        else // AND 
        {
            // x -> (a & b)  ==> !x | (a & b) ==> (!x | a) & (!x | b)
            var positives = new HashSet<string>();
            var negatives = new HashSet<string> { tseitin.Name };
            if (leftSense) positives.Add(leftName); else negatives.Add(leftName);
            _clauses.Add((positives, negatives));
            positives = [];
            negatives = [tseitin.Name];
            if (rightSense) positives.Add(rightName); else negatives.Add(rightName);
            _clauses.Add((positives, negatives));             
        }

        return tseitin;
    }

    /// <summary>
    /// Transforms the given <see cref="BooleanExpression"/> into a conjunctive normal form.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to transform.</param>
    /// <returns>A <see cref="BooleanExpression"/> representing the conjunctive normal form of the original <paramref name="expression"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <c>null</c>.</exception>
    public static BooleanExpression Transform(BooleanExpression expression)
    {
        var transformer = new TseitinTransformer();
        var resultingExpression = transformer.Rewrite(Lowerer.Lower(expression));
        if (resultingExpression.Kind == ExpressionKind.Constant || 
            resultingExpression.Kind == ExpressionKind.Unary ||
            resultingExpression.Kind == ExpressionKind.Literal && !((LiteralExpression)resultingExpression).Name.StartsWith(".t", StringComparison.InvariantCulture))
            return resultingExpression;

        return resultingExpression.And(transformer._clauses.Select(clause =>
            clause.positives.Select(name => (BooleanExpression)Literal(name))
                .Concat(clause.negatives.Select(name => Not(Literal(name)))).Aggregate((e1, e2) => e1.Or(e2))).Aggregate((e1, e2) => e1.And(e2)));
    }
}
