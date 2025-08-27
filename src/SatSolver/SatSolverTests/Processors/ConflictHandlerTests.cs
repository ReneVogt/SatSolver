using Moq;
using Revo.SatSolver;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace SatSolverTests.Processors;

public sealed class ConflictHandlerTests
{
    [Theory]
    [
        InlineData(2, 4),
        InlineData(3, 4),
        InlineData(0, 2)
    ]
    public void OmittedLearnedConstraint(int minimum, int maximum)
    {
        var options = new SatSolverOptions() { MaximumLiteralBlockDistance = maximum, ConstraintDeletion = new() { LiteralBlockDistanceToKeep = minimum } };
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].DecisionLevel = 1;
        variables[0].Sense = true;
        variables[1].DecisionLevel = 10;
        variables[1].Sense = true;
        variables[2].DecisionLevel = 3;
        variables[2].Sense = false;
        variables[3].DecisionLevel = 3;
        variables[3].Sense = true;
        variables[4].DecisionLevel = 1;
        variables[4].Sense = false;

        var activityManager = new Mock<IManageActivities>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var propagationRateTracker = new Mock<ITrackPropagationRate>(MockBehavior.Strict);
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>(MockBehavior.Strict);
        var learnedConstraintCreator = new Mock<ICreateLearnedConstraints>(MockBehavior.Strict);
        var learnedConstraints = new List<Constraint>();
        var unitPropagationQueue = new UnitPropagationQueue();
        unitPropagationQueue.Enqueue((variables[0].PositiveLiteral, null));
        var restartManager = new Mock<IManageRestart>(MockBehavior.Strict);
        var constraintMinimizer = new Mock<IMinimizeConstraints>(MockBehavior.Strict);

        var sut = new ConflictHandler<IManageActivities, IVariableTrail, ITrackPropagationRate, ITrackLiteralBlockDistance, ICreateLearnedConstraints, IManageRestart, IMinimizeConstraints>(options, variables, activityManager.Object, trail.Object, propagationRateTracker.Object,
            literalBlockDistanceTracker.Object, learnedConstraintCreator.Object, learnedConstraints, unitPropagationQueue, restartManager.Object, constraintMinimizer.Object);

        var conflictingConstraint = new Constraint([variables[0].PositiveLiteral, variables[1].NegativeLiteral]);

        var sequence = new MockSequence();
        propagationRateTracker.InSequence(sequence).Setup(p => p.AddConflict());
        restartManager.InSequence(sequence).Setup(rm => rm.AddConflict());
        activityManager.InSequence(sequence).Setup(am => am.IncreaseConstraintActivity(conflictingConstraint, 1));

        trail.InSequence(sequence).Setup(t => t.DecisionLevel).Returns(10);

        StampArray? learnedLiterals = null;
        learnedConstraintCreator.InSequence(sequence)
            .Setup(lcc => lcc.CreateLearnedConstraint(conflictingConstraint, It.IsAny<StampArray>()))
            .Callback<Constraint, StampArray>((c, target) => 
            {
                learnedLiterals = target;
                target.Clear();
                target.Add(variables[0].NegativeLiteral.StampIndex);
                target.Add(variables[1].NegativeLiteral.StampIndex);
                target.Add(variables[2].PositiveLiteral.StampIndex);
                target.Add(variables[3].NegativeLiteral.StampIndex);
                target.Add(variables[4].PositiveLiteral.StampIndex);
            });
        constraintMinimizer.InSequence(sequence)
            .Setup(cm => cm.MinimizeConstraint(It.IsAny<StampArray>(), 10, It.IsAny<ConstraintLiteral[]>()))
            .Callback<StampArray, int, ConstraintLiteral[]>((target, dl, ls) => Assert.Equal(target, learnedLiterals));
        activityManager.InSequence(sequence).Setup(am => am.ConstraintActivityIncrement).Returns(17);

        Constraint? constraintWithIncreasedVariableActivity = null;
        activityManager.InSequence(sequence)
            .Setup(am => am.IncreaseVariableActivity(It.IsAny<Constraint>()))
            .Callback<Constraint>(c => constraintWithIncreasedVariableActivity = c);
        Constraint? constraintWithIncreasedConstraintActivity = null;
        activityManager.InSequence(sequence)
            .Setup(am => am.IncreaseConstraintActivity(It.IsAny<Constraint>(), 1))
            .Callback<Constraint, double>((c, f) => constraintWithIncreasedConstraintActivity = c);
        activityManager.InSequence(sequence)
            .Setup(am => am.DecayConstraintActivity());

        literalBlockDistanceTracker.InSequence(sequence).Setup(lbd => lbd.AddValue(3));

        // reset uip sense
        trail.InSequence(sequence).Setup(t => t.JumpBack(3)).Callback(() => variables[1].Sense = null);

        sut.HandleConflict(conflictingConstraint);

        activityManager.VerifyAll();
        trail.VerifyAll();
        propagationRateTracker.VerifyAll();
        literalBlockDistanceTracker.VerifyAll();
        restartManager.VerifyAll();
        constraintMinimizer.VerifyAll();
        learnedConstraintCreator.VerifyAll();
        activityManager.VerifyNoOtherCalls();
        trail.VerifyNoOtherCalls();
        propagationRateTracker.VerifyNoOtherCalls();
        literalBlockDistanceTracker.VerifyNoOtherCalls();
        restartManager.VerifyNoOtherCalls();
        constraintMinimizer.VerifyNoOtherCalls();
        learnedConstraintCreator.VerifyNoOtherCalls();

        var (unitLiteral, learnedConstraint) = Assert.Single(unitPropagationQueue);
        Assert.Equal(variables[1].NegativeLiteral, unitLiteral);
        Assert.NotNull(learnedConstraint);
        Assert.Equal(3, learnedConstraint.LiteralBlockDistance);
        Assert.True(learnedConstraint.IsLearned);
        Assert.Equal(3 > maximum, learnedConstraint.IsOmitted);
        Assert.Equal(3 > minimum && 3 <= maximum, learnedConstraint.IsTracked);
        Assert.Equal(17, learnedConstraint.Activity);

        Assert.Equal(learnedConstraint, constraintWithIncreasedVariableActivity);
        Assert.Equal(learnedConstraint, constraintWithIncreasedConstraintActivity);
    }

    [Fact]
    public void SingleLiteralInLearnedConstraint()
    {
        var options = new SatSolverOptions() { MaximumLiteralBlockDistance = 2, ConstraintDeletion = new() { LiteralBlockDistanceToKeep = 4 } };
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[2].DecisionLevel = 3;
        variables[2].Sense = false;

        var activityManager = new Mock<IManageActivities>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var propagationRateTracker = new Mock<ITrackPropagationRate>(MockBehavior.Strict);
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>(MockBehavior.Strict);
        var learnedConstraintCreator = new Mock<ICreateLearnedConstraints>(MockBehavior.Strict);
        var learnedConstraints = new List<Constraint>();
        var unitPropagationQueue = new UnitPropagationQueue();
        unitPropagationQueue.Enqueue((variables[0].PositiveLiteral, null));
        var restartManager = new Mock<IManageRestart>(MockBehavior.Strict);
        var constraintMinimizer = new Mock<IMinimizeConstraints>(MockBehavior.Strict);

        var sut = new ConflictHandler<IManageActivities, IVariableTrail, ITrackPropagationRate, ITrackLiteralBlockDistance, ICreateLearnedConstraints, IManageRestart, IMinimizeConstraints>(options, variables, activityManager.Object, trail.Object, propagationRateTracker.Object,
            literalBlockDistanceTracker.Object, learnedConstraintCreator.Object, learnedConstraints, unitPropagationQueue, restartManager.Object, constraintMinimizer.Object);

        var conflictingConstraint = new Constraint([variables[0].PositiveLiteral, variables[1].NegativeLiteral]);

        var sequence = new MockSequence();
        propagationRateTracker.InSequence(sequence).Setup(p => p.AddConflict());
        restartManager.InSequence(sequence).Setup(rm => rm.AddConflict());
        activityManager.InSequence(sequence).Setup(am => am.IncreaseConstraintActivity(conflictingConstraint, 1));

        trail.InSequence(sequence).Setup(t => t.DecisionLevel).Returns(3);

        StampArray? learnedLiterals = null;
        learnedConstraintCreator.InSequence(sequence)
            .Setup(lcc => lcc.CreateLearnedConstraint(conflictingConstraint, It.IsAny<StampArray>()))
            .Callback<Constraint, StampArray>((c, target) =>
            {
                learnedLiterals = target;
                target.Clear();
                target.Add(variables[2].PositiveLiteral.StampIndex);
            });
        constraintMinimizer.InSequence(sequence)
            .Setup(cm => cm.MinimizeConstraint(It.IsAny<StampArray>(), 3, It.IsAny<ConstraintLiteral[]>()))
            .Callback<StampArray, int, ConstraintLiteral[]>((target, dl, ls) => Assert.Equal(target, learnedLiterals));
        activityManager.InSequence(sequence).Setup(am => am.ConstraintActivityIncrement).Returns(17);

        Constraint? constraintWithIncreasedVariableActivity = null;
        activityManager.InSequence(sequence)
            .Setup(am => am.IncreaseVariableActivity(It.IsAny<Constraint>()))
            .Callback<Constraint>(c => constraintWithIncreasedVariableActivity = c);
        Constraint? constraintWithIncreasedConstraintActivity = null;
        activityManager.InSequence(sequence)
            .Setup(am => am.IncreaseConstraintActivity(It.IsAny<Constraint>(), 1))
            .Callback<Constraint, double>((c, f) => constraintWithIncreasedConstraintActivity = c);
        activityManager.InSequence(sequence)
            .Setup(am => am.DecayConstraintActivity());

        literalBlockDistanceTracker.InSequence(sequence).Setup(lbd => lbd.AddValue(1));

        // reset uip sense
        trail.InSequence(sequence).Setup(t => t.JumpBack(0)).Callback(() => variables[2].Sense = null);

        sut.HandleConflict(conflictingConstraint);

        activityManager.VerifyAll();
        trail.VerifyAll();
        propagationRateTracker.VerifyAll();
        literalBlockDistanceTracker.VerifyAll();
        restartManager.VerifyAll();
        constraintMinimizer.VerifyAll();
        learnedConstraintCreator.VerifyAll();
        activityManager.VerifyNoOtherCalls();
        trail.VerifyNoOtherCalls();
        propagationRateTracker.VerifyNoOtherCalls();
        literalBlockDistanceTracker.VerifyNoOtherCalls();
        restartManager.VerifyNoOtherCalls();
        constraintMinimizer.VerifyNoOtherCalls();
        learnedConstraintCreator.VerifyNoOtherCalls();

        var (unitLiteral, learnedConstraint) = Assert.Single(unitPropagationQueue);
        Assert.Equal(variables[2].PositiveLiteral, unitLiteral);
        Assert.NotNull(learnedConstraint);
        Assert.Equal(1, learnedConstraint.LiteralBlockDistance);
        Assert.True(learnedConstraint.IsLearned);

        Assert.Equal(learnedConstraint, constraintWithIncreasedVariableActivity);
        Assert.Equal(learnedConstraint, constraintWithIncreasedConstraintActivity);
    }
}
