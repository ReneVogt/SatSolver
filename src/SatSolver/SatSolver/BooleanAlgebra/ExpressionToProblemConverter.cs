using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Net.Http.Headers;

namespace Revo.SatSolver.BooleanAlgebra;

/// <summary>
/// Converts a <see cref="BooleanExpression"/> into a
/// conjunctive normal form, tries to reduce redundancies
/// and returns the expression as a <see cref="Problem"/>
/// to be processed the <see cref="SatSolver"/>.
/// </summary>
public sealed class ExpressionToProblemConverter : BooleanExpressionRewriter
{

    readonly Dictionary<string, int> _literalMapping = [];
    readonly HashSet<int> _positives = [];
    readonly HashSet<int> _negatives = [];
    readonly List<Clause> _clauses = [];

    ExpressionToProblemConverter() { }

    public override BooleanExpression RewriteLiteralExpression(LiteralExpression expression)
    {
        _positives.Add(GetLiteralId(expression.Name));
        return expression;
    }
    public override BooleanExpression RewriteUnaryExpression(UnaryExpression expression)
    {
        _negatives.Add(GetLiteralId(((LiteralExpression)expression.Expression).Name));
        return expression;
    }

    public override BooleanExpression RewriteBinaryExpression(BinaryExpression expression)
    {
        if (expression.Operator != BinaryOperator.And) return base.RewriteBinaryExpression(expression);

        _positives.Clear();
        _negatives.Clear();
        var left = Rewrite(expression.Left);
        if (left.Kind != ExpressionKind.Binary || ((BinaryExpression)left).Operator != BinaryOperator.And)        
            _clauses.Add(new(_positives.Select(p => new Literal(p, true)).Concat(_negatives.Select(n => new Literal(n, false)))));

        _positives.Clear();
        _negatives.Clear();
        var right = Rewrite(expression.Right);
        if (right.Kind != ExpressionKind.Binary || ((BinaryExpression)right).Operator != BinaryOperator.And)
            _clauses.Add(new(_positives.Select(p => new Literal(p, true)).Concat(_negatives.Select(n => new Literal(n, false)))));

        return expression;
    }

    int GetLiteralId(string name)
    {
        if (!_literalMapping.TryGetValue(name, out var id))
        {
            id = _literalMapping.Count+1;
            _literalMapping.Add(name, id);
        }
        return id;
    }

    /// <summary>
    /// Converts a <see cref="BooleanExpression"/> into a
    /// conjunctive normal form, tries to reduce redundancies
    /// and returns the expression as a <see cref="Problem"/>
    /// to be processed the <see cref="SatSolver"/>.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to transform.</param>
    /// <returns>A <see cref="Problem"/> representation of the <paramref name="expression"/>
    /// that can be processed by the <see cref="SatSolver"/>.</returns>
    /// <exception cref="ArgumentNullException">The <paramref name="expression"/> is <c>null</c>.</exception>
    public static Problem ToProblem(BooleanExpression expression) => ToProblem(expression, out _);

    /// <summary>
    /// Converts a <see cref="BooleanExpression"/> into a
    /// conjunctive normal form, tries to reduce redundancies
    /// and returns the expression as a <see cref="Problem"/>
    /// to be processed the <see cref="SatSolver"/>.
    /// </summary>
    /// <param name="expression">The <see cref="BooleanExpression"/> to transform.</param>
    /// <param name="literalMapping">Receives the mapping from variable names to literal IDs.</param>
    /// <returns>A <see cref="Problem"/> representation of the <paramref name="expression"/>
    /// that can be processed by the <see cref="SatSolver"/>.</returns>
    /// <exception cref="ArgumentNullException">The <paramref name="expression"/> is <c>null</c>.</exception>
    public static Problem ToProblem(BooleanExpression expression, out IReadOnlyDictionary<string, int> literalMapping)
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression));
        var normalized = ConjunctiveNormalFormTransformer.Transform(expression);
        var reduced = RedundancyReducer.Reduce(normalized);
        if (reduced.Kind == ExpressionKind.Constant)
        {
            literalMapping = new Dictionary<string, int>().AsReadOnly();
            return ((ConstantExpression)reduced).Sense
                ? new(0, Enumerable.Empty<Clause>())
                : new(0, [new Clause(Enumerable.Empty<Literal>())]);
        }
        if (reduced.Kind == ExpressionKind.Literal)
        {
            literalMapping = new Dictionary<string, int>() { [((LiteralExpression)reduced).Name] = 1 }.AsReadOnly();
            return new(1, [new Clause([1])]);
        }
        if (reduced.Kind == ExpressionKind.Unary)
        {
            literalMapping = new Dictionary<string, int>() { [((LiteralExpression)((UnaryExpression)reduced).Expression).Name] = 1 }.AsReadOnly();
            return new(1, [new Clause([-1])]);
        }

        var converter = new ExpressionToProblemConverter();        
        converter.Rewrite(reduced);
        literalMapping = converter._literalMapping.AsReadOnly();
        return new (converter._literalMapping.Count, converter._clauses);
    }
}
