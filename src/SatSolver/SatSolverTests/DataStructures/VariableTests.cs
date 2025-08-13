using Revo.SatSolver.DataStructures;

namespace SatSolverTests.DataStructures;

public sealed class VariableTests
{
    [Fact]
    public void CorrectLiteralInitialization()
    {
        var variable = new Variable(12);
        Assert.Equal(12, variable.Index);
        Assert.Equal(variable, variable.PositiveLiteral.Variable);
        Assert.Equal(variable, variable.NegativeLiteral.Variable);

        Assert.True(variable.PositiveLiteral.Orientation);
        Assert.False(variable.NegativeLiteral.Orientation);
        Assert.Null(variable.Sense);
        Assert.Null(variable.PositiveLiteral.Sense);
        Assert.Null(variable.NegativeLiteral.Sense);
    }
    [Fact]
    public void CorrectHashCodes()
    {
        var variable = new Variable(12);
        Assert.Equal(12, variable.GetHashCode());
        Assert.Equal(13, variable.PositiveLiteral.GetHashCode());
        Assert.Equal(-13, variable.NegativeLiteral.GetHashCode());


        variable = new Variable(0);
        Assert.Equal(0, variable.GetHashCode());
        Assert.Equal(1, variable.PositiveLiteral.GetHashCode());
        Assert.Equal(-1, variable.NegativeLiteral.GetHashCode());
    }
    [Fact]
    public void SensePropagated()
    {
        var variable = new Variable(12);
        Assert.Null(variable.Sense);
        Assert.Null(variable.PositiveLiteral.Sense);
        Assert.Null(variable.NegativeLiteral.Sense);

        variable.Sense = true;
        Assert.True(variable.Sense);
        Assert.True(variable.PositiveLiteral.Sense);
        Assert.False(variable.NegativeLiteral.Sense);

        variable.Sense = false;
        Assert.False(variable.Sense);
        Assert.False(variable.PositiveLiteral.Sense);
        Assert.True(variable.NegativeLiteral.Sense);

        variable.Sense = null;
        Assert.Null(variable.Sense);
        Assert.Null(variable.PositiveLiteral.Sense);
        Assert.Null(variable.NegativeLiteral.Sense);
    }
}
