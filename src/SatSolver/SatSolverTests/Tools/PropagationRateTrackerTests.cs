using Revo.SatSolver.Tools;

namespace SatSolverTests.Tools;

public sealed class PropagationRateTrackerTests
{
    [Fact]
    public void NumericStabilityOnZero()
    {
        var sut = new PropagationRateTracker(1, 100, 0.7, 5, 10);
        sut.AddConflict();
        Assert.Equal(1, sut.CurrentRatio);
    }
    [Fact]
    public void Test()
    {
        const double threshold = 0.7d;

        var sut = new PropagationRateTracker(1, 100, threshold, 5, 10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.False(sut.ShouldRestart());

        AddRate(10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.False(sut.ShouldRestart());
        AddRate(10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.False(sut.ShouldRestart());
        AddRate(10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.False(sut.ShouldRestart());
        AddRate(10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.False(sut.ShouldRestart());
        AddRate(10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.False(sut.ShouldRestart());

        AddRates(10, 10, 10, 10);
        Assert.Equal(1, sut.CurrentRatio);
        Assert.False(sut.ShouldRestart());

        AddRates(5, 5, 5, 5, 5);
        Assert.True(sut.CurrentRatio < threshold);
        Assert.False(sut.ShouldRestart());

        AddRate(5);
        Assert.True(sut.CurrentRatio < threshold);
        Assert.True(sut.ShouldRestart());

        sut.ResetAfterRestart();
        Assert.False(sut.ShouldRestart());

        AddRates(2, 2, 2, 2, 2, 2, 2, 2, 2);
        Assert.False(sut.ShouldRestart());

        AddRate(1);
        Assert.True(sut.CurrentRatio < threshold);
        Assert.True(sut.ShouldRestart());

        void AddRates(params int[] props)
        {
            foreach (var p in props) AddRate(p);
        }
        void AddRate(int props)
        {
            for (var i = 0; i<props; i++)
                sut.AddPropagation();
            sut.AddConflict();
        }
    }
}