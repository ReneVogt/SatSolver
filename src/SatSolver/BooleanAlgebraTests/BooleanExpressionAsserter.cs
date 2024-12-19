using Revo.BooleanAlgebra.Expressions;

namespace BooleanAlgebraTests;

sealed class BooleanExpressionAsserter(BooleanExpression expression) : IDisposable
{
    readonly IEnumerator<object> enumerator = Flatten(expression).GetEnumerator();
    bool hasErrors;

    public void Dispose()
    {
        if (!hasErrors)
            Assert.False(enumerator.MoveNext(), "More expressions than expected.");
        enumerator.Dispose();
    }

    static IEnumerable<object> Flatten(BooleanExpression expression)
    {
        Stack<BooleanExpression> stack = new();
        stack.Push(expression);

        while (stack.Count > 0)
        {
            var e = stack.Pop();
            yield return e;
            switch (e)
            {
                case BinaryExpression binaryExpression:
                    stack.Push(binaryExpression.Right);
                    stack.Push(binaryExpression.Left);
                    break;
                case UnaryExpression unaryExpression:
                    stack.Push(unaryExpression.Expression);
                    break;

            }
        }
    }

    bool Markfailed() => !(hasErrors = true);

    public void AssertConstant(bool sense)
    {
        try
        {
            Assert.True(enumerator.MoveNext());
            var constantExpression = Assert.IsType<ConstantExpression>(enumerator.Current);
            Assert.Equal(sense, constantExpression.Sense);
        }
        catch when (Markfailed())
        {
            throw;
        }
    }
    public void AssertLiteral(string name)
    {
        try
        {
            Assert.True(enumerator.MoveNext());
            var literalExpression = Assert.IsType<LiteralExpression>(enumerator.Current);
            Assert.Equal(name, literalExpression.Name);
        }
        catch when (Markfailed())
        {
            throw;
        }
    }
    public void AssertEquivalence()
    {
        try
        {
            Assert.True(enumerator.MoveNext());
            var binaryExpression = Assert.IsType<BinaryExpression>(enumerator.Current);
            Assert.Equal(BinaryOperator.Equivalence, binaryExpression.Operator);
        }
        catch when (Markfailed())
        {
            throw;
        }
    }
    public void AssertImplication()
    {
        try
        {
            Assert.True(enumerator.MoveNext());
            var binaryExpression = Assert.IsType<BinaryExpression>(enumerator.Current);
            Assert.Equal(BinaryOperator.Implication, binaryExpression.Operator);
        }
        catch when (Markfailed())
        {
            throw;
        }
    }
    public void AssertReverseImplication()
    {
        try
        {
            Assert.True(enumerator.MoveNext());
            var binaryExpression = Assert.IsType<BinaryExpression>(enumerator.Current);
            Assert.Equal(BinaryOperator.ReverseImplication, binaryExpression.Operator);
        }
        catch when (Markfailed())
        {
            throw;
        }
    }
    public void AssertOr()
    {
        try
        {
            Assert.True(enumerator.MoveNext());
            var binaryExpression = Assert.IsType<BinaryExpression>(enumerator.Current);
            Assert.Equal(BinaryOperator.Or, binaryExpression.Operator);
        }
        catch when (Markfailed())
        {
            throw;
        }
    }
    public void AssertXor()
    {
        try
        {
            Assert.True(enumerator.MoveNext());
            var binaryExpression = Assert.IsType<BinaryExpression>(enumerator.Current);
            Assert.Equal(BinaryOperator.Xor, binaryExpression.Operator);
        }
        catch when (Markfailed())
        {
            throw;
        }
    }
    public void AssertAnd()
    {
        try
        {
            Assert.True(enumerator.MoveNext());
            var binaryExpression = Assert.IsType<BinaryExpression>(enumerator.Current);
            Assert.Equal(BinaryOperator.And, binaryExpression.Operator);
        }
        catch when (Markfailed())
        {
            throw;
        }
    }
    public void AssertNot()
    {
        try
        {
            Assert.True(enumerator.MoveNext());
            var unaryExpression = Assert.IsType<UnaryExpression>(enumerator.Current);
            Assert.Equal(UnaryOperator.Not, unaryExpression.Operator);
        }
        catch when (Markfailed())
        {
            throw;
        }
    }
}
