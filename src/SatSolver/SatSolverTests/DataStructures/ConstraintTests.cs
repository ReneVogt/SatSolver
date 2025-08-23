using Revo.SatSolver.DataStructures;

namespace SatSolverTests.DataStructures;

public sealed class ConstraintTests
{
    [Fact]
    public void InitialConstructor_SingleLiteral_WatcherConnectedOnce()
    {
        var variable = new Variable(17);
        var sut = new Constraint([variable.PositiveLiteral]);

        Assert.Equal(variable.PositiveLiteral, sut.Watched1);
        Assert.Equal(variable.PositiveLiteral, sut.Watched2);
        Assert.Contains(sut, variable.PositiveLiteral.Watchers);
        Assert.Single(variable.PositiveLiteral.Watchers);
        Assert.Empty(variable.NegativeLiteral.Watchers);

        Assert.Equal([variable.PositiveLiteral], sut.Literals);

        Assert.Equal(0, sut.LiteralBlockDistance);
        Assert.Equal(0, sut.Activity);
        Assert.False(sut.IsTracked);
        Assert.False(sut.IsLearned);
    }
    [Fact]
    public void InitialConstructor_MultipleLiteral_WatchersConnected()
    {
        var v1 = new Variable(17);
        var v2 = new Variable(42);
        var v3 = new Variable(23);
        var sut = new Constraint([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral]);

        Assert.Equal(v1.PositiveLiteral, sut.Watched1);
        Assert.Equal(v2.NegativeLiteral, sut.Watched2);
        
        Assert.Contains(sut, v1.PositiveLiteral.Watchers);
        Assert.Single(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);

        Assert.Contains(sut, v2.NegativeLiteral.Watchers);
        Assert.Single(v2.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);

        Assert.Empty(v3.PositiveLiteral.Watchers);
        Assert.Empty(v3.NegativeLiteral.Watchers);

        Assert.Equal(0, sut.LiteralBlockDistance);
        Assert.Equal(0, sut.Activity);
        Assert.False(sut.IsTracked);
        Assert.False(sut.IsLearned);

        Assert.Equal([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral], sut.Literals);
    }

    [Fact]
    public void SolutionConstructor_SingleWatcher_WatcherConnectedOnce()
    {
        var variable = new Variable(17);
        var sut = new Constraint([variable.PositiveLiteral], variable.PositiveLiteral, variable.PositiveLiteral);

        Assert.Equal(variable.PositiveLiteral, sut.Watched1);
        Assert.Equal(variable.PositiveLiteral, sut.Watched2);
        Assert.Contains(sut, variable.PositiveLiteral.Watchers);
        Assert.Single(variable.PositiveLiteral.Watchers);
        Assert.Empty(variable.NegativeLiteral.Watchers);

        Assert.Equal([variable.PositiveLiteral], sut.Literals);

        Assert.Equal(0, sut.LiteralBlockDistance);
        Assert.Equal(0, sut.Activity);
        Assert.False(sut.IsTracked);
        Assert.True(sut.IsLearned);
    }
    [Fact]
    public void SolutionConstructor_MultipleLiteral_WatchersConnected()
    {
        var v1 = new Variable(17);
        var v2 = new Variable(42);
        var v3 = new Variable(23);
        var sut = new Constraint([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral],
            v2.NegativeLiteral, v3.NegativeLiteral);

        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);

        Assert.Equal(v2.NegativeLiteral, sut.Watched1);
        Assert.Contains(sut, v2.NegativeLiteral.Watchers);
        Assert.Single(v2.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);

        Assert.Equal(v3.NegativeLiteral, sut.Watched2);
        Assert.Contains(sut, v3.NegativeLiteral.Watchers);
        Assert.Single(v3.NegativeLiteral.Watchers);
        Assert.Empty(v3.PositiveLiteral.Watchers);

        Assert.Equal(0, sut.LiteralBlockDistance);
        Assert.Equal(0, sut.Activity);
        Assert.False(sut.IsTracked);
        Assert.True(sut.IsLearned);

        Assert.Equal([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral], sut.Literals);
    }

    [Fact]
    public void LearnedConstructor_SingleWatcher_Connect_WatcherConnectedOnce()
    {
        const double activity = 3.1416d;
        const int lbd = 3;

        var variable = new Variable(17);
        var sut = new Constraint([variable.PositiveLiteral], variable.PositiveLiteral, variable.PositiveLiteral, activity, lbd, true);

        Assert.Equal(variable.PositiveLiteral, sut.Watched1);
        Assert.Equal(variable.PositiveLiteral, sut.Watched2);
        Assert.Contains(sut, variable.PositiveLiteral.Watchers);
        Assert.Single(variable.PositiveLiteral.Watchers);
        Assert.Empty(variable.NegativeLiteral.Watchers);

        Assert.Equal([variable.PositiveLiteral], sut.Literals);

        Assert.Equal(lbd, sut.LiteralBlockDistance);
        Assert.Equal(activity, sut.Activity);
        Assert.False(sut.IsTracked);
        Assert.True(sut.IsLearned);
    }
    [Fact]
    public void LearnedConstructor_SingleWatcher_DontConnect_WatchersNotConnected()
    {
        const double activity = 3.1416d;
        const int lbd = 3;

        var variable = new Variable(17);
        var sut = new Constraint([variable.PositiveLiteral], variable.PositiveLiteral, variable.PositiveLiteral, activity, lbd, false);

        Assert.Equal(variable.PositiveLiteral, sut.Watched1);
        Assert.Equal(variable.PositiveLiteral, sut.Watched2);
        Assert.Empty(variable.PositiveLiteral.Watchers);
        Assert.Empty(variable.NegativeLiteral.Watchers);

        Assert.Equal([variable.PositiveLiteral], sut.Literals);

        Assert.Equal(lbd, sut.LiteralBlockDistance);
        Assert.Equal(activity, sut.Activity);
        Assert.False(sut.IsTracked);
        Assert.True(sut.IsLearned);
    }
    [Fact]
    public void LearnedConstructor_MultipleLiteral_Connect_WatchersConnected()
    {
        const double activity = 3.1416d;
        const int lbd = 3;

        var v1 = new Variable(17);
        var v2 = new Variable(42);
        var v3 = new Variable(23);
        var sut = new Constraint([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral],
            v2.NegativeLiteral, v3.NegativeLiteral,
            activity, lbd, true);

        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);

        Assert.Equal(v2.NegativeLiteral, sut.Watched1);
        Assert.Contains(sut, v2.NegativeLiteral.Watchers);
        Assert.Single(v2.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);

        Assert.Equal(v3.NegativeLiteral, sut.Watched2);
        Assert.Contains(sut, v3.NegativeLiteral.Watchers);
        Assert.Single(v3.NegativeLiteral.Watchers);
        Assert.Empty(v3.PositiveLiteral.Watchers);

        Assert.Equal(lbd, sut.LiteralBlockDistance);
        Assert.Equal(activity, sut.Activity);
        Assert.False(sut.IsTracked);
        Assert.True(sut.IsLearned);

        Assert.Equal([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral], sut.Literals);
    }
    [Fact]
    public void LearnedConstructor_MultipleLiteral_DontConnect_WatchersNotConnected()
    {
        const double activity = 3.1416d;
        const int lbd = 3;

        var v1 = new Variable(17);
        var v2 = new Variable(42);
        var v3 = new Variable(23);
        var sut = new Constraint([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral],
            v2.NegativeLiteral, v3.NegativeLiteral,
            activity, lbd, false);

        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);

        Assert.Equal(v2.NegativeLiteral, sut.Watched1);
        Assert.Empty(v2.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);

        Assert.Equal(v3.NegativeLiteral, sut.Watched2);
        Assert.Empty(v3.NegativeLiteral.Watchers);
        Assert.Empty(v3.PositiveLiteral.Watchers);

        Assert.Equal(lbd, sut.LiteralBlockDistance);
        Assert.Equal(activity, sut.Activity);
        Assert.False(sut.IsTracked);
        Assert.True(sut.IsLearned);

        Assert.Equal([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral], sut.Literals);
    }

    [Fact]
    public void ToString_DimacsClauseNotation()
    {
        var v1 = new Variable(17);
        var v2 = new Variable(42);
        var v3 = new Variable(23);
        var sut = new Constraint([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral]);

        Assert.Equal("18 -43 -24", sut.ToString());
    }
}