using Revo.SatSolver.Tools;

namespace SatSolverTests.Tools;

public sealed class LiteralBlockDistanceTrackerTests
{
    [Fact]
    public void NumericStabilityOnZero()
    {
        var sut = new LiteralBlockDistanceTracker(1, 100, 1.3, 5, 10);
        sut.AddLiteralBlockDistance(0);
        Assert.Equal(0, sut.Average, 0.01);
        Assert.Equal(1, sut.CurrentRatio);
    }
    [Fact]
    public void Test()
    {
        const double threshold = 1.3d;

        var sut = new LiteralBlockDistanceTracker(1, 100, threshold, 5, 10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.False(sut.ShouldRestart());
        Assert.Equal(0, sut.Average, 0.01);

        sut.AddLiteralBlockDistance(10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.Equal(10, sut.Average, 0.01);
        Assert.False(sut.ShouldRestart());
        sut.AddLiteralBlockDistance(10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.Equal(10, sut.Average, 0.01);
        Assert.False(sut.ShouldRestart());
        sut.AddLiteralBlockDistance(10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.Equal(10, sut.Average, 0.01);
        Assert.False(sut.ShouldRestart());
        sut.AddLiteralBlockDistance(10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.Equal(10, sut.Average, 0.01);
        Assert.False(sut.ShouldRestart());
        sut.AddLiteralBlockDistance(10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.Equal(10, sut.Average, 0.01);
        Assert.False(sut.ShouldRestart());

        AddLbds(10, 10, 10, 10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.Equal(10, sut.Average, 0.01);
        Assert.False(sut.ShouldRestart());

        AddLbds(15, 15, 15, 15, 15);
        Assert.True(sut.CurrentRatio > threshold);
        Assert.Equal(10.17, sut.Average, 0.01);
        Assert.False(sut.ShouldRestart());

        sut.AddLiteralBlockDistance(15);
        Assert.True(sut.CurrentRatio > threshold);
        Assert.Equal(10.2, sut.Average, 0.01);
        Assert.True(sut.ShouldRestart());

        sut.ResetAfterRestart();
        Assert.False(sut.ShouldRestart());

        AddLbds(20, 20, 20, 20, 20, 20, 20, 20, 20);
        Assert.Equal(10.8, sut.Average, 0.01);
        Assert.False(sut.ShouldRestart());

        sut.AddLiteralBlockDistance(30);
        Assert.Equal(10.93, sut.Average, 0.01);
        Assert.True(sut.CurrentRatio > threshold);
        Assert.True(sut.ShouldRestart());

        void AddLbds(params int[] lbds)
        {
            foreach (var lbd in lbds) sut.AddLiteralBlockDistance(lbd);
        }
    }
}
