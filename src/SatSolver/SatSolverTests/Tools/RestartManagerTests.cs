using Moq;
using Revo.SatSolver;
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
        var options = new SatSolverOptions
        {
            Restart = new()
            {
                Interval = null,
                Luby = false,
                ByLiteralBlockDistance = false,
                ByPropagationRate = false
            },
            ConstraintDeletion = new()
            {
                ReduceOnRestart = false
            }
        };

        var propagationRateTracker = new Mock<ITrackPropagationRate>();
#if DEBUG
        propagationRateTracker.Setup(p => p.CurrentRatio).Returns(17000);
#endif
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>();
        literalBlockDistanceTracker.Setup(l => l.CurrentRatio).Returns(17000);
        var constraintReducer = new Mock<IReduceLearnedConstraints>();
        var trail = new Mock<IVariableTrail>();
        var sut = new RestartManager<IVariableTrail, ITrackPropagationRate, ITrackLiteralBlockDistance, IReduceLearnedConstraints, LubySequence>(
            options,
            trail.Object,
            propagationRateTracker.Object,
            literalBlockDistanceTracker.Object,
            [],
            constraintReducer.Object,
            null);

        Assert.False(sut.RestartIfNecessary());
        for (var i = 0; i<1000; i++) sut.AddConflict();
        Assert.False(sut.RestartIfNecessary());
        constraintReducer.VerifyNoOtherCalls();
        trail.VerifyNoOtherCalls();
        propagationRateTracker.VerifyNoOtherCalls();
    }
    [Fact]
    public void Restart_OnInterval_ConstraintReducer()
    {
        var options = new SatSolverOptions
        {
            Restart = new()
            {
                Interval = 23,
                Luby = false,
                ByLiteralBlockDistance = false,
                ByPropagationRate = false
            },
            ConstraintDeletion = new()
            {
                ReduceOnRestart = true
            }
        };

        var propagationRateTracker = new Mock<ITrackPropagationRate>();
#if DEBUG
        propagationRateTracker.Setup(p => p.CurrentRatio).Returns(17000);
#endif
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>();
        literalBlockDistanceTracker.Setup(l => l.CurrentRatio).Returns(17000);
        var constraintReducer = new Mock<IReduceLearnedConstraints>();
        var trail = new Mock<IVariableTrail>();
        var sut = new RestartManager<IVariableTrail, ITrackPropagationRate, ITrackLiteralBlockDistance, IReduceLearnedConstraints, LubySequence>(
            options,
            trail.Object,
            propagationRateTracker.Object,
            literalBlockDistanceTracker.Object,
            [],
            constraintReducer.Object,
            null);

        Assert.False(sut.RestartIfNecessary());
        for (var i = 0; i<23; i++)
        {
            sut.AddConflict();
            Assert.False(sut.RestartIfNecessary());
        }

        propagationRateTracker.VerifyNoOtherCalls();
        sut.AddConflict();
        Assert.True(sut.RestartIfNecessary());
        constraintReducer.Verify(cr => cr.ReduceLearnedConstraints(), Times.Once);
        constraintReducer.VerifyNoOtherCalls();
        trail.Verify(t => t.JumpBack(0), Times.Once);
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

        trail.Verify(t => t.JumpBack(0), Times.Exactly(2));
        trail.VerifyNoOtherCalls();
        Assert.False(sut.RestartIfNecessary());

        constraintReducer.VerifyNoOtherCalls();
        trail.VerifyNoOtherCalls();
    }
    [Fact]
    public void Restart_OnLuby_NoConstraintReducer()
    {
        var options = new SatSolverOptions
        {
            Restart = new()
            {
                Interval = 1,
                Luby = true,
                ByLiteralBlockDistance = false,
                ByPropagationRate = false
            },
            ConstraintDeletion = new()
            {
                ReduceOnRestart = false
            }
        };

        var propagationRateTracker = new Mock<ITrackPropagationRate>();
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>();
        literalBlockDistanceTracker.Setup(l => l.CurrentRatio).Returns(17000);
        var constraintReducer = new Mock<IReduceLearnedConstraints>();
        var trail = new Mock<IVariableTrail>();

        var sut = new RestartManager<IVariableTrail, ITrackPropagationRate, ITrackLiteralBlockDistance, IReduceLearnedConstraints, ILubySequence>(
            options,
            trail.Object,
            propagationRateTracker.Object,
            literalBlockDistanceTracker.Object,
            [],
            constraintReducer.Object,
            new LubyMock([5L, 20L, 100L]));

        Assert.False(sut.RestartIfNecessary());
        propagationRateTracker.VerifyAll();
        propagationRateTracker.VerifyNoOtherCalls();
        for (var i = 0; i<5; i++)
        {
            sut.AddConflict();
            Assert.False(sut.RestartIfNecessary());
            propagationRateTracker.VerifyNoOtherCalls();
        }

        sut.AddConflict();
        Assert.True(sut.RestartIfNecessary());
#if DEBUG
        propagationRateTracker.Verify(p => p.CurrentRatio, Times.Once);
#endif
        propagationRateTracker.Verify(p => p.ResetAfterRestart(), Times.Once);
        propagationRateTracker.VerifyNoOtherCalls();

        trail.Verify(t => t.JumpBack(0), Times.Once);
        trail.VerifyNoOtherCalls();
        Assert.False(sut.RestartIfNecessary());
        for (var i = 0; i<20; i++)
        {
            sut.AddConflict();
            Assert.False(sut.RestartIfNecessary());
            propagationRateTracker.VerifyNoOtherCalls();
        }
        propagationRateTracker.VerifyAll();
        propagationRateTracker.VerifyNoOtherCalls();
        sut.AddConflict();
        Assert.True(sut.RestartIfNecessary());
#if DEBUG
        propagationRateTracker.Verify(p => p.CurrentRatio, Times.Exactly(2));
#endif
        propagationRateTracker.Verify(p => p.ResetAfterRestart(), Times.Exactly(2));
        propagationRateTracker.VerifyNoOtherCalls();
        trail.Verify(t => t.JumpBack(0), Times.Exactly(2));
        trail.VerifyNoOtherCalls();
        Assert.False(sut.RestartIfNecessary());
        propagationRateTracker.VerifyNoOtherCalls();
        constraintReducer.VerifyNoOtherCalls();
        trail.VerifyNoOtherCalls();
    }
    [Fact]
    public void Restart_OnPropagationRate_ConstraintReducer()
    {
        var options = new SatSolverOptions
        {
            Restart = new()
            {
                Interval = null,
                Luby = false,
                ByLiteralBlockDistance = false,
                ByPropagationRate = true
            },
            ConstraintDeletion = new()
            {
                ReduceOnRestart = true
            }
        };

        var propagationRateTracker = new Mock<ITrackPropagationRate>();
        propagationRateTracker.SetupSequence(p => p.ShouldRestart())
            .Returns(false)
            .Returns(true)
            .Returns(false)
            .Returns(true);
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>();
        literalBlockDistanceTracker.Setup(l => l.CurrentRatio).Returns(1);
        var constraintReducer = new Mock<IReduceLearnedConstraints>();
        var trail = new Mock<IVariableTrail>();
        var sut = new RestartManager<IVariableTrail, ITrackPropagationRate, ITrackLiteralBlockDistance, IReduceLearnedConstraints, ILubySequence>(
            options,
            trail.Object,
            propagationRateTracker.Object,
            literalBlockDistanceTracker.Object,
            [],
            constraintReducer.Object,
            null);

        Assert.False(sut.RestartIfNecessary());
        propagationRateTracker.VerifyAll();
        propagationRateTracker.VerifyNoOtherCalls();
        Assert.True(sut.RestartIfNecessary());
#if DEBUG
        propagationRateTracker.Verify(p => p.CurrentRatio, Times.Once);
#endif
        propagationRateTracker.Verify(p => p.ResetAfterRestart(), Times.Once);
        propagationRateTracker.VerifyAll();
        propagationRateTracker.VerifyNoOtherCalls();
        constraintReducer.Verify(c => c.ReduceLearnedConstraints(), Times.Once());
        constraintReducer.VerifyNoOtherCalls();
        trail.Verify(t => t.JumpBack(0), Times.Once);
        trail.VerifyNoOtherCalls();

        Assert.False(sut.RestartIfNecessary());

        Assert.True(sut.RestartIfNecessary());
#if DEBUG
        propagationRateTracker.Verify(p => p.CurrentRatio, Times.Exactly(2));
#endif
        propagationRateTracker.Verify(p => p.ResetAfterRestart(), Times.Exactly(2));
        propagationRateTracker.VerifyAll();
        propagationRateTracker.VerifyNoOtherCalls();
        constraintReducer.Verify(c => c.ReduceLearnedConstraints(), Times.Exactly(2));
        constraintReducer.VerifyNoOtherCalls();
        trail.Verify(t => t.JumpBack(0), Times.Exactly(2));
        trail.VerifyNoOtherCalls();
    }
    [Fact]
    public void Restart_OnLiteralBlockDistance_NoConstraintReducer()
    {
        var options = new SatSolverOptions
        {
            Restart = new()
            {
                Interval = null,
                Luby = false,
                ByLiteralBlockDistance = true,
                ByPropagationRate = false
            },
            ConstraintDeletion = new()
            {
                ReduceOnRestart = true
            }
        };

        var propagationRateTracker = new Mock<ITrackPropagationRate>();
        propagationRateTracker.Setup(p => p.CurrentRatio).Returns(1);
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>();
        literalBlockDistanceTracker.SetupSequence(l => l.ShouldRestart())
            .Returns(false)
            .Returns(true)
            .Returns(false)
            .Returns(true);
        var constraintReducer = new Mock<IReduceLearnedConstraints>();
        var trail = new Mock<IVariableTrail>();
        var sut = new RestartManager<IVariableTrail, ITrackPropagationRate, ITrackLiteralBlockDistance, IReduceLearnedConstraints, ILubySequence>(
            options,
            trail.Object,
            propagationRateTracker.Object,
            literalBlockDistanceTracker.Object,
            [],
            constraintReducer.Object,
            null);

        Assert.False(sut.RestartIfNecessary());
        literalBlockDistanceTracker.Verify(l => l.ResetAfterRestart(), Times.Never);
        Assert.True(sut.RestartIfNecessary());
        literalBlockDistanceTracker.Verify(l => l.ResetAfterRestart(), Times.Once);
        trail.Verify(t => t.JumpBack(0), Times.Once);
        trail.VerifyNoOtherCalls();

        Assert.False(sut.RestartIfNecessary());
        literalBlockDistanceTracker.Verify(l => l.ResetAfterRestart(), Times.Once);
        Assert.True(sut.RestartIfNecessary());
        literalBlockDistanceTracker.Verify(l => l.ResetAfterRestart(), Times.Exactly(2));
        trail.Verify(t => t.JumpBack(0), Times.Exactly(2));
        trail.VerifyNoOtherCalls();

        literalBlockDistanceTracker.VerifyAll();
    }
}
