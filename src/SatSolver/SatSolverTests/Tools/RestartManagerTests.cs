using Moq;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace SatSolverTests.Tools;

public sealed class RestartManagerTests
{
    sealed class LubyMock(IEnumerable<long> sequence) : ILubySequence
    {
        readonly IEnumerator<long> _enumerator = sequence.GetEnumerator();
        public static IEnumerable<long> Enumerate(long baseValue = 1) => throw new NotImplementedException();
        public long Next()
        {
            if (!_enumerator.MoveNext()) throw new InvalidOperationException("No more data!");
            return _enumerator.Current;
        }
    }
    [Fact]
    public void NoTriggers_NoRestart()
    {
        var propagationRateTracker = new Mock<ITrackPropagationRate>();
        propagationRateTracker.Setup(p => p.CurrentRatio).Returns(17000);
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>();
        literalBlockDistanceTracker.Setup(l => l.CurrentRatio).Returns(17000);
        var constraintReducer = new Mock<IReduceLearnedConstraints>();
        var trail = new Mock<IVariableTrail>();
        var sut = new RestartManager(
            trail.Object,
            propagationRateTracker.Object,
            literalBlockDistanceTracker.Object,
            [],
            constraintReducer.Object,
            null,
            null,
            null,
            null,
            false);

        Assert.False(sut.RestartIfNecessary());
        for (var i = 0; i<1000; i++) sut.AddConflict();
        Assert.False(sut.RestartIfNecessary());
        constraintReducer.VerifyNoOtherCalls();
        trail.VerifyNoOtherCalls();
    }
    [Fact]
    public void Restart_OnInterval_ConstraintReducer()
    {
        var propagationRateTracker = new Mock<ITrackPropagationRate>();
        propagationRateTracker.Setup(p => p.CurrentRatio).Returns(17000);
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>();
        literalBlockDistanceTracker.Setup(l => l.CurrentRatio).Returns(17000);
        var constraintReducer = new Mock<IReduceLearnedConstraints>();
        var trail = new Mock<IVariableTrail>();
        var sut = new RestartManager(
            trail.Object,
            propagationRateTracker.Object,
            literalBlockDistanceTracker.Object,
            [],
            constraintReducer.Object,
            23,
            null,
            null,
            null,
            true);

        Assert.False(sut.RestartIfNecessary());
        for (var i = 0; i<23; i++)
        {
            sut.AddConflict();
            Assert.False(sut.RestartIfNecessary());
        }

        sut.AddConflict();
        Assert.True(sut.RestartIfNecessary());
        constraintReducer.Verify(cr => cr.ReduceLearnedConstraints(), Times.Once);
        constraintReducer.VerifyNoOtherCalls();
        trail.Verify(t => t.Reset(), Times.Once);
        trail.VerifyNoOtherCalls();
        Assert.False(sut.RestartIfNecessary());

        for (var i = 0; i<23; i++)
        {
            sut.AddConflict();
            Assert.False(sut.RestartIfNecessary());
        }
        sut.AddConflict();
        Assert.True(sut.RestartIfNecessary());
        constraintReducer.Verify(cr => cr.ReduceLearnedConstraints(), Times.Exactly(2));
        constraintReducer.VerifyNoOtherCalls();
        
        trail.Verify(t => t.Reset(), Times.Exactly(2));
        trail.VerifyNoOtherCalls();
        Assert.False(sut.RestartIfNecessary());

        constraintReducer.VerifyNoOtherCalls();
        trail.VerifyNoOtherCalls();
    }
    [Fact]
    public void Restart_OnLuby_NoConstraintReducer()
    {
        var propagationRateTracker = new Mock<ITrackPropagationRate>();
        propagationRateTracker.Setup(p => p.CurrentRatio).Returns(17000);
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>();
        literalBlockDistanceTracker.Setup(l => l.CurrentRatio).Returns(17000);
        var constraintReducer = new Mock<IReduceLearnedConstraints>();
        var trail = new Mock<IVariableTrail>();

        var sut = new RestartManager(
            trail.Object,
            propagationRateTracker.Object,
            literalBlockDistanceTracker.Object,
            [],
            constraintReducer.Object,
            null,
            new LubyMock([5L, 20L, 100L]),
            null,
            null,
            false);

        Assert.False(sut.RestartIfNecessary());
        for (var i = 0; i<5; i++)
        {
            sut.AddConflict();
            Assert.False(sut.RestartIfNecessary());
        }

        sut.AddConflict();
        Assert.True(sut.RestartIfNecessary());

        trail.Verify(t => t.Reset(), Times.Once);
        trail.VerifyNoOtherCalls();
        Assert.False(sut.RestartIfNecessary());
        for (var i = 0; i<20; i++)
        {
            sut.AddConflict();
            Assert.False(sut.RestartIfNecessary());
        }
        sut.AddConflict();
        Assert.True(sut.RestartIfNecessary());
        trail.Verify(t => t.Reset(), Times.Exactly(2));
        trail.VerifyNoOtherCalls();
        Assert.False(sut.RestartIfNecessary());

        constraintReducer.VerifyNoOtherCalls();
        trail.VerifyNoOtherCalls();
    }
    [Fact]
    public void Restart_OnPropagationRate_ConstraintReducer()
    {
        var propagationRateTracker = new Mock<ITrackPropagationRate>();
        propagationRateTracker.SetupSequence(p => p.CurrentRatio)
            .Returns(1)
            .Returns(0.4)
            .Returns(0.5)
            .Returns(0.49);
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>();
        literalBlockDistanceTracker.Setup(l => l.CurrentRatio).Returns(1);
        var constraintReducer = new Mock<IReduceLearnedConstraints>();
        var trail = new Mock<IVariableTrail>();
        var sut = new RestartManager(
            trail.Object,
            propagationRateTracker.Object,
            literalBlockDistanceTracker.Object,
            [],
            constraintReducer.Object,
            null,
            null,
            0.5,
            null,
            true);

        Assert.False(sut.RestartIfNecessary());
        Assert.True(sut.RestartIfNecessary());
        constraintReducer.Verify(c => c.ReduceLearnedConstraints(), Times.Once());
        constraintReducer.VerifyNoOtherCalls();
        trail.Verify(t => t.Reset(), Times.Once);
        trail.VerifyNoOtherCalls();

        Assert.False(sut.RestartIfNecessary());

        Assert.True(sut.RestartIfNecessary());
        constraintReducer.Verify(c => c.ReduceLearnedConstraints(), Times.Exactly(2));
        constraintReducer.VerifyNoOtherCalls();
        trail.Verify(t => t.Reset(), Times.Exactly(2));
        trail.VerifyNoOtherCalls();

        propagationRateTracker.VerifyAll();
    }
    [Fact]
    public void Restart_OnLiteralBlockDistance_NoConstraintReducer()
    {
        var propagationRateTracker = new Mock<ITrackPropagationRate>();
        propagationRateTracker.Setup(p => p.CurrentRatio).Returns(1);
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>();
        literalBlockDistanceTracker.SetupSequence(l => l.CurrentRatio)
            .Returns(1)
            .Returns(1.6)
            .Returns(1.5)
            .Returns(1.51);
        var constraintReducer = new Mock<IReduceLearnedConstraints>();
        var trail = new Mock<IVariableTrail>();
        var sut = new RestartManager(
            trail.Object,
            propagationRateTracker.Object,
            literalBlockDistanceTracker.Object,
            [],
            constraintReducer.Object,
            null,
            null,
            null,
            1.5,
            false);

        Assert.False(sut.RestartIfNecessary());
        Assert.True(sut.RestartIfNecessary());
        trail.Verify(t => t.Reset(), Times.Once);
        trail.VerifyNoOtherCalls();

        Assert.False(sut.RestartIfNecessary());
        Assert.True(sut.RestartIfNecessary());
        trail.Verify(t => t.Reset(), Times.Exactly(2));
        trail.VerifyNoOtherCalls();

        literalBlockDistanceTracker.VerifyAll();
    }
}
