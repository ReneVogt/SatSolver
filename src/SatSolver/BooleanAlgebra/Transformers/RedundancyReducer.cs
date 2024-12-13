using Revo.BooleanAlgebra.Expressions;
using static Revo.BooleanAlgebra.Expressions.ExpressionFactory;

namespace Revo.BooleanAlgebra.Transformers;

/// <summary>
/// Reduces redundancies in <see cref="BooleanExpression"/>s.
/// </summary>
/// <remarks>
/// We collect child expressions along OR and AND expressions.
/// Literals that occure more than once in either expression
/// type are removed. If they occure in different senses
/// (negated and not negated), a containing OR expression
/// is autmatically true and a containing AND expression
/// is automatically false.
/// For AND expressions we collect the information about
/// which literals are contained in the child expressions
/// so that we can remove parts (OR expressions) that 
/// contain a superset of literals in another OR expression
/// that has no other nested expressions.
/// E.g.: (a | b) & (a | b | c) => a | b
/// but (a | b | (c % d)) & (a | b | c) cannot be reduced./// 
/// </remarks>
public class RedundancyReducer : BooleanExpressionRewriter
{
    sealed class State
    {
        public List<OrChild> OrChildren { get; } = [];
        public List<AndChild> AndChildren { get; } = [];
        public string? Literal { get; set; }
        public bool Sense { get; set; }
    }

    State _state = new();

    sealed record OrChild(BooleanExpression Expression, string? Literal, bool Sense);
    sealed record AndChild(BooleanExpression Expression, HashSet<string> Positives, HashSet<string> Negatives, bool HasNestedExpressions)
    {
        public bool HasLiterals => Positives.Count + Negatives.Count > 0;
    }

    RedundancyReducer() { }

    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)
    {
        if (expression.Operator != UnaryOperator.Not) return base.RewriteUnaryExpression(expression);

        var innerExpression = Rewrite(expression.Expression, out var innerState);

        //
        // Handle the inner expression.
        // 
        switch (innerExpression.Kind)
        {
            case ExpressionKind.Constant:
                return ((ConstantExpression)innerExpression).Sense ? Zero : One;

            case ExpressionKind.Literal:
                //
                // The literal is negated, add it to the _negatives.
                // 
                _state.Literal = innerState.Literal;
                _state.Sense = !innerState.Sense;
                break;

            case ExpressionKind.Unary:
                //
                // Double negation return rewritten 
                // inner expression.
                //
                if (((UnaryExpression)innerExpression).Operator != UnaryOperator.Not)
                    break;
                return ((UnaryExpression)innerExpression).Expression;

            default:
                //
                // This must be a binary that is negated
                // by the current expression, so it will
                // stay as a nested expression and not
                // add to the literal sets.
                //
                break;
        }

        return innerExpression == expression.Expression ? expression : Not(innerExpression);
    }
    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression) => expression.Operator switch
    {
        BinaryOperator.And => RewriteAndExpression(expression),
        BinaryOperator.Xor => RewriteXorExpression(expression),
        BinaryOperator.Or => RewriteOrExpression(expression),
        _ => base.RewriteBinaryExpression(expression)
    };

    BooleanExpression RewriteAndExpression(BinaryExpression expression)
    {
        //
        // Rewrite left child.
        //
        var left = Rewrite(expression.Left, out var leftState);

        //
        // Check for constants.
        //
        if (left.Kind == ExpressionKind.Constant)
            return ((ConstantExpression)left).Sense
                ? Rewrite(expression.Right)
                : Zero;
        //
        // Rewrite right child.
        //
        var right = Rewrite(expression.Right, out var rightState);

        //
        // Check for constants.
        //
        if (right.Kind == ExpressionKind.Constant)
        {
            if (!((ConstantExpression)right).Sense)
                return Zero;
            _state = leftState;
            return left;
        }

        //
        // Merge expressions into state.
        //
        if (MergeAndChildren(left, leftState)) return Zero;
        if (MergeAndChildren(right, rightState)) return Zero;

        //
        // Regenerate optimized AND expression from state.
        //
        var resultingExpression = _state.AndChildren.Select(child => child.Expression).Aggregate((e1, e2) => e1.And(e2));
        if (_state.AndChildren.Count == 1)
        {
            //
            // We optimized so heavily that
            // this is no longer an AND expression
            // and the caller expects different
            // states for that.
            // 
            _state = new();
            return Rewrite(resultingExpression);
        }
        return resultingExpression;
    }
    bool MergeAndChildren(BooleanExpression expression, State state)
    {
        foreach (var addedChild in state.AndChildren)
            if (CombineAndExpression(addedChild)) return true;

        if (expression.Kind != ExpressionKind.Binary)
        {
            var child = new AndChild(expression, [], [], state.Literal is null);
            if (state.Literal is not null)
                if (state.Sense)
                    child.Positives.Add(state.Literal);
                else
                    child.Negatives.Add(state.Literal);

            return CombineAndExpression(child);
        }

        var binary = (BinaryExpression)expression;

        if (binary.Operator == BinaryOperator.And) return false;
        if (binary.Operator != BinaryOperator.Or)
            return CombineAndExpression(new(expression, [], [], true));

        var nestedOrChild = new AndChild(expression, [], [], state.OrChildren.Any(orChild => orChild.Literal is null));
        foreach (var pos in state.OrChildren.Where(orc => orc.Literal is not null && orc.Sense).Select(orc => orc.Literal!))
            nestedOrChild.Positives.Add(pos);
        foreach (var neg in state.OrChildren.Where(orc => orc.Literal is not null && !orc.Sense).Select(orc => orc.Literal!))
            nestedOrChild.Negatives.Add(neg);
        return CombineAndExpression(nestedOrChild);
    }
    bool CombineAndExpression(AndChild child)
    {
        //
        // We check for single literal expressions that
        // contradict each other and return true if we
        // found one, making the whole expression Zero.
        //
        if (!child.HasNestedExpressions &&
            child.Negatives.Count == 0 && child.Positives.Count == 1 && _state.AndChildren.Any(previousChild => !previousChild.HasNestedExpressions && previousChild.Positives.Count == 0 && previousChild.Negatives.SetEquals(child.Positives)) ||
            child.Negatives.Count == 1 && child.Positives.Count == 0 && _state.AndChildren.Any(previousChild => !previousChild.HasNestedExpressions && previousChild.Negatives.Count == 0 && previousChild.Positives.SetEquals(child.Negatives)))
            return true;

        //
        // Check if the new expression is a superset
        // of an already existing one.
        // 
        if (child.HasLiterals && _state.AndChildren.Any(previousChild => !previousChild.HasNestedExpressions && child.Positives.IsSupersetOf(previousChild.Positives) && child.Negatives.IsSupersetOf(previousChild.Negatives)))
            return false;

        //
        // Now we remove existing supersets of the
        // new expression before adding it.
        //
        if (!child.HasNestedExpressions)
            _state.AndChildren.RemoveAll(previousChild => previousChild.Positives.IsSupersetOf(child.Positives) && previousChild.Negatives.IsSupersetOf(child.Negatives));
        _state.AndChildren.Add(child);
        return false;
    }

    BooleanExpression RewriteXorExpression(BinaryExpression expression)
    {
        //
        // Rewrite left child.
        //
        var left = Rewrite(expression.Left, out var leftState);
        //
        // Check for constants.
        //
        if (left.Kind == ExpressionKind.Constant)
            return ((ConstantExpression)left).Sense ? Rewrite(Not(expression.Right)) : Rewrite(expression.Right);

        //
        // Rewrite right child.
        //
        var right = Rewrite(expression.Right, out var rightState);
        //
        // Check for constants.
        //
        if (right.Kind == ExpressionKind.Constant)
        {
            if (((ConstantExpression)right).Sense)
                return Rewrite(Not(expression.Left));
            _state = leftState;
            return left;
        }

        //
        // Check for XOR between simple or negated literals
        //
        if (leftState.Literal is not null && leftState.Literal == rightState.Literal)
            return leftState.Sense == rightState.Sense ? Zero : One;

        //
        // Return Xor.
        //
        return left == expression.Left && right == expression.Right
            ? expression
            : left.Xor(right);
    }
    BooleanExpression RewriteOrExpression(BinaryExpression expression)
    {
        //
        // Rewrite left child.
        //
        var left = Rewrite(expression.Left, out var leftState);

        //
        // Check for constants.
        //
        if (left.Kind == ExpressionKind.Constant)
            return ((ConstantExpression)left).Sense
                ? One
                : Rewrite(expression.Right);

        //
        // Rewrite right child.
        //
        var right = Rewrite(expression.Right, out var rightState);

        //
        // Check for constants.
        //
        if (right.Kind == ExpressionKind.Constant)
        {
            if (((ConstantExpression)right).Sense)
                return One;
            _state = leftState;
            return left;
        }


        //
        // Merge expressions into _orChildren.
        //
        foreach (var child in leftState.OrChildren)
            if (CombineOrExpression(child)) return One;
        if ((left.Kind != ExpressionKind.Binary || ((BinaryExpression)left).Operator != BinaryOperator.Or) &&
            CombineOrExpression(new(left, leftState.Literal, leftState.Sense)))
            return One;
        foreach (var child in rightState.OrChildren)
            if (CombineOrExpression(child)) return One;
        if ((right.Kind != ExpressionKind.Binary || ((BinaryExpression)right).Operator != BinaryOperator.Or) &&
            CombineOrExpression(new(right, rightState.Literal, rightState.Sense)))
            return One;

        //
        // Regenerate optimized OR expression from state.
        //
        var resultingExpression = _state.OrChildren.Select(child => child.Expression).Aggregate((e1, e2) => e1.Or(e2));
        if (_state.OrChildren.Count == 1)
        {
            //
            // We optimized so heavily that
            // this is no longer an OR expression
            // and the caller expects different
            // states for that.
            // 
            _state = new();
            return Rewrite(resultingExpression);
        }
        return resultingExpression;
    }

    bool CombineOrExpression(OrChild child)
    {
        //
        // If the expression is complex,
        // we simply add it to our collection.
        //
        if (child.Literal is null)
        {
            _state.OrChildren.Add(child);
            return false;
        }

        //
        // We check if we already know this literal (so
        // we don't add it twice) or if we know it in
        // a different sense which would lead to an
        // immediate true.
        //
        var knowingExpression = _state.OrChildren.FirstOrDefault(previousExpression => previousExpression.Literal == child.Literal);
        if (knowingExpression is { Sense: var sense }) return sense != child.Sense;

        _state.OrChildren.Add(child);
        return false;
    }

    public override BooleanExpression RewriteLiteralExpression(LiteralExpression expression)
    {
        _state.Literal = expression.Name;
        _state.Sense = true;
        return expression;
    }

    BooleanExpression Rewrite(BooleanExpression expression, out State state)
    {
        var savedState = _state;
        _state = new();
        try
        {
            return Rewrite(expression);
        }
        finally
        {
            state = _state;
            _state = savedState;
        }
    }

    /// <summary>
    /// Removes redundancies from a boolean <paramref name="expression"/> expression.
    /// Removes redundant literals (as in 'a | a' or 'a & a'), converts tautologies
    /// (like 'a | !a') and contradictions (like 'a & !a') to the respective constant
    /// expressions and removes supersets of literals in OR-expressions (like 
    /// '(a | b) & (a | b | c) => (a | b)').
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to reduce.</param>
    /// <returns>A <see cref="BooleanExpression"/> representing the reduced expression tree.</returns>
    /// <exception cref="ArgumentNullException">The <paramref name="expression"/> is <c>null</c>.</exception>
    public static BooleanExpression Reduce(BooleanExpression expression) =>
        new RedundancyReducer().Rewrite(expression);
}
