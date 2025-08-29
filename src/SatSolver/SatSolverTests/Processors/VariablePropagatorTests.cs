using Moq;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace SatSolverTests.Processors;

public sealed class VariablePropagatorTests
{
    static readonly ConstraintFactory _constraintFactory = new([]);

    [Fact]
    public void PropagateVariable_NoConflict_NoPropagations()
    {
        // We use the 2of3 problem
        // p cnf 3 8
        // 1 2 3 0
        // -1 -2 -3 0
        // -1 2 3 0
        // 1 -2 3 0
        // 1 2 -3 0
        // 1 2 0
        // 1 3 0
        // 2 3 0

        var variables = Enumerable.Range(0, 3).Select(i => new Variable(i)).ToArray();
        var constraint0 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint1 = _constraintFactory.CreateInitialConstraint([variables[0].NegativeLiteral, variables[1].NegativeLiteral, variables[2].NegativeLiteral]);
        var constraint2 = _constraintFactory.CreateInitialConstraint([variables[0].NegativeLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint3 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[1].NegativeLiteral, variables[2].PositiveLiteral]);
        var constraint4 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].NegativeLiteral]);
        var constraint5 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral]);
        var constraint6 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint7 = _constraintFactory.CreateInitialConstraint([variables[1].PositiveLiteral, variables[2].PositiveLiteral]);

        var trail = new Mock<IVariableTrail>();
        var activityManager = new Mock<IManageActivities>();
        var units = new UnitPropagationQueue();
        var propagationRateTracker = new Mock<ITrackPropagationRate>();

        var sut = new VariablePropagator<IVariableTrail, IManageActivities, ITrackPropagationRate>(trail.Object, units, activityManager.Object, propagationRateTracker.Object);

        var conflict = sut.PropagateVariable(variables[0], true, null);
        Assert.Null(conflict);

        trail.Verify(t => t.Add(variables[0]), Times.Once);
        trail.VerifyNoOtherCalls();
        Assert.True(variables[0].Sense);
        Assert.True(variables[0].Polarity);

        Assert.Equal(variables[1].NegativeLiteral, constraint1.Watched1);
        Assert.Equal(variables[2].NegativeLiteral, constraint1.Watched2);

        Assert.Equal(variables[1].PositiveLiteral, constraint2.Watched1);
        Assert.Equal(variables[2].PositiveLiteral, constraint2.Watched2);

        activityManager.VerifyNoOtherCalls();
        Assert.Empty(units);
        propagationRateTracker.VerifyNoOtherCalls();        
    }
    [Fact]
    public void PropagateVariable_NoConflict_CorrectPropagations()
    {
        // We use the 2of3 problem
        // p cnf 3 8
        // 1 2 3 0
        // -1 -2 -3 0
        // -1 2 3 0
        // 1 -2 3 0
        // 1 2 -3 0
        // 1 2 0
        // 1 3 0
        // 2 3 0

        var variables = Enumerable.Range(0, 3).Select(i => new Variable(i)).ToArray();
        var constraint0 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint1 = _constraintFactory.CreateInitialConstraint([variables[0].NegativeLiteral, variables[1].NegativeLiteral, variables[2].NegativeLiteral]);
        var constraint2 = _constraintFactory.CreateInitialConstraint([variables[0].NegativeLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint3 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[1].NegativeLiteral, variables[2].PositiveLiteral]);
        var constraint4 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].NegativeLiteral]);
        var constraint5 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral]);
        var constraint6 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint7 = _constraintFactory.CreateInitialConstraint([variables[1].PositiveLiteral, variables[2].PositiveLiteral]);

        var trail = new Mock<IVariableTrail>();
        var activityManager = new Mock<IManageActivities>();
        var sequence = new MockSequence();
        activityManager.InSequence(sequence).Setup(a => a.IncreaseConstraintActivity(constraint6, 0.5d));
        activityManager.InSequence(sequence).Setup(a => a.IncreaseConstraintActivity(constraint5, 0.5d));

        var units = new UnitPropagationQueue();
        var propagationRateTracker = new Mock<ITrackPropagationRate>();

        var sut = new VariablePropagator<IVariableTrail, IManageActivities, ITrackPropagationRate>(trail.Object, units, activityManager.Object, propagationRateTracker.Object);

        var conflict = sut.PropagateVariable(variables[0], false, null);
        Assert.Null(conflict);

        trail.Verify(t => t.Add(variables[0]), Times.Once);
        trail.VerifyNoOtherCalls();
        Assert.False(variables[0].Sense);
        Assert.False(variables[0].Polarity);

        Assert.Equal(variables[1].PositiveLiteral, constraint0.Watched1);
        Assert.Equal(variables[2].PositiveLiteral, constraint0.Watched2);

        Assert.Equal(variables[1].NegativeLiteral, constraint3.Watched1);
        Assert.Equal(variables[2].PositiveLiteral, constraint3.Watched2);

        Assert.Equal(variables[1].PositiveLiteral, constraint4.Watched1);
        Assert.Equal(variables[2].NegativeLiteral, constraint4.Watched2);

        Assert.Equal(variables[1].PositiveLiteral, constraint5.Watched1);
        Assert.Equal(variables[0].PositiveLiteral, constraint5.Watched2);

        Assert.Equal(variables[2].PositiveLiteral, constraint6.Watched1);
        Assert.Equal(variables[0].PositiveLiteral, constraint6.Watched2);

        activityManager.VerifyAll();
        activityManager.VerifyNoOtherCalls();
        Assert.Equal([(variables[2].PositiveLiteral, constraint6), (variables[1].PositiveLiteral, constraint5)], units);
        propagationRateTracker.Verify(p => p.AddPropagation(), Times.Exactly(2));       
    }
    [Fact]
    public void PropagateVariable_WithConflict()
    {
        // p cnf 3 2
        // 1 2 3 0
        // -1 -2 -3 0

        var variables = Enumerable.Range(0, 3).Select(i => new Variable(i)).ToArray();
        variables[1].Sense = true;
        variables[2].Sense = true;
        var constraint0 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint1 = _constraintFactory.CreateInitialConstraint([variables[0].NegativeLiteral, variables[1].NegativeLiteral, variables[2].NegativeLiteral]);

        var trail = new Mock<IVariableTrail>();
        var activityManager = new Mock<IManageActivities>();
        var units = new UnitPropagationQueue();
        var propagationRateTracker = new Mock<ITrackPropagationRate>();

        var sut = new VariablePropagator<IVariableTrail, IManageActivities, ITrackPropagationRate>(trail.Object, units, activityManager.Object, propagationRateTracker.Object);

        var conflict = sut.PropagateVariable(variables[0], true, null);
        Assert.Equal(constraint1, conflict);

        trail.Verify(t => t.Add(variables[0]), Times.Once);
        trail.VerifyNoOtherCalls();
        Assert.True(variables[0].Sense);
        Assert.False(variables[0].Polarity);

        Assert.Equal(variables[0].PositiveLiteral, constraint0.Watched1);
        Assert.Equal(variables[1].PositiveLiteral, constraint0.Watched2);

        Assert.Equal(variables[1].NegativeLiteral, constraint1.Watched1);
        Assert.Equal(variables[0].NegativeLiteral, constraint1.Watched2);

        activityManager.VerifyNoOtherCalls();
        Assert.Empty(units);
        propagationRateTracker.Verify(p => p.AddPropagation(), Times.Never);
    }
    [Fact]
    public void PropagateVariable_AlreadyTrueConstraints()
    {
        // p cnf 3 2
        // 1 2 3 0
        // 1 3 2 0

        var variables = Enumerable.Range(0, 3).Select(i => new Variable(i)).ToArray();
        variables[2].Sense = true;
        var constraint0 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint1 = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral, variables[2].PositiveLiteral, variables[1].PositiveLiteral]);

        var trail = new Mock<IVariableTrail>();
        var activityManager = new Mock<IManageActivities>();
        var units = new UnitPropagationQueue();
        var propagationRateTracker = new Mock<ITrackPropagationRate>();

        var sut = new VariablePropagator<IVariableTrail, IManageActivities, ITrackPropagationRate>(trail.Object, units, activityManager.Object, propagationRateTracker.Object);

        var conflict = sut.PropagateVariable(variables[0], false, null);
        Assert.Null(conflict);

        trail.Verify(t => t.Add(variables[0]), Times.Once);
        trail.VerifyNoOtherCalls();
        Assert.False(variables[0].Sense);
        Assert.False(variables[0].Polarity);

        Assert.Equal(variables[1].PositiveLiteral, constraint0.Watched1);
        Assert.Equal(variables[2].PositiveLiteral, constraint0.Watched2);

        Assert.Equal(variables[2].PositiveLiteral, constraint1.Watched1);
        Assert.Equal(variables[0].PositiveLiteral, constraint1.Watched2);

        activityManager.VerifyNoOtherCalls();
        Assert.Empty(units);
        propagationRateTracker.Verify(p => p.AddPropagation(), Times.Never);
    }
    [Fact]
    public void PropagateVariable_FalsifiedUnitConstraint_Conflict()
    {
        // p cnf 1 2
        // 1 0
        // -1 0

        var variables = new[] { new Variable(1) };
        _ = _constraintFactory.CreateInitialConstraint([variables[0].PositiveLiteral]);
        var constraint1 = _constraintFactory.CreateInitialConstraint([variables[0].NegativeLiteral]);

        var trail = new Mock<IVariableTrail>();
        var activityManager = new Mock<IManageActivities>();
        var units = new UnitPropagationQueue();
        var propagationRateTracker = new Mock<ITrackPropagationRate>();

        var sut = new VariablePropagator<IVariableTrail, IManageActivities, ITrackPropagationRate>(trail.Object, units, activityManager.Object, propagationRateTracker.Object);

        var conflict = sut.PropagateVariable(variables[0], true, null);
        Assert.Equal(constraint1, conflict);
    }
}
