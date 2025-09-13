using Moq;
using Revo.SatSolver;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

#pragma warning disable IDE0079
#pragma warning disable CA1861

namespace SatSolverTests.Processors;

public sealed class ConflictHandlerTests
{
    [Fact]
    public void HandleConflict_CorrectProcess()
    {
        const int maxLBD = 117;
        const int minLBD = 118;
        const int lbd = 17;
        const int decisionLevel = 10;
        const int activity = 23;
        const int expectedJumpBackLevel = 37;

        var options = new SatSolverOptions() { MaximumLiteralBlockDistance = maxLBD, ConstraintDeletion = new() { LiteralBlockDistanceToKeep = minLBD } };
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i) { Sense = true }).ToArray();
        var literals = variables.SelectMany(v => new[] { v.PositiveLiteral, v.NegativeLiteral }).ToArray();
        var activityManager = new Mock<IManageActivities>(MockBehavior.Strict);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var propagationRateTracker = new Mock<ITrackPropagationRate>(MockBehavior.Strict);
        var literalBlockDistanceTracker = new Mock<ITrackLiteralBlockDistance>(MockBehavior.Strict);
        var learnedConstraintCreator = new Mock<ICreateLearnedConstraints>(MockBehavior.Strict);
        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);

        var conflictingConstraint = new Constraint([.. variables.Select(v => v.NegativeLiteral)], variables[0].NegativeLiteral, variables[1].NegativeLiteral);
        var learnedConstraint = new Constraint([variables[1].NegativeLiteral, variables[3].NegativeLiteral], variables[1].NegativeLiteral, variables[2].NegativeLiteral)
        {
            LiteralBlockDistance = lbd
        };

        var unitPropagationQueue = new UnitPropagationQueue();
        unitPropagationQueue.Enqueue((variables[0].PositiveLiteral, null));
        var restartManager = new Mock<IManageRestart>(MockBehavior.Strict);
        var constraintMinimizer = new Mock<IMinimizeConstraints>(MockBehavior.Strict);

        var sut = new ConflictHandler<IManageActivities, IVariableTrail, ITrackPropagationRate, ITrackLiteralBlockDistance, ICreateLearnedConstraints, IManageRestart, IMinimizeConstraints, IConstraintFactory>(options, literals, activityManager.Object, trail.Object, propagationRateTracker.Object,
            literalBlockDistanceTracker.Object, learnedConstraintCreator.Object, unitPropagationQueue, restartManager.Object, constraintMinimizer.Object, constraintFactory.Object, new Statistics(null, null));

        var sequence = new MockSequence();
        propagationRateTracker.InSequence(sequence).Setup(p => p.AddConflict());
        restartManager.InSequence(sequence).Setup(rm => rm.AddConflict());
        activityManager.InSequence(sequence).Setup(am => am.IncreaseConstraintActivity(conflictingConstraint, 1));

        trail.InSequence(sequence).Setup(t => t.DecisionLevel).Returns(decisionLevel);

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
            .Setup(cm => cm.MinimizeConstraint(It.IsAny<StampArray>(), decisionLevel, It.IsAny<ConstraintLiteral[]>()))
            .Callback<StampArray, int, ConstraintLiteral[]>((target, dl, ls) => Assert.Equal(target, learnedLiterals));
        activityManager.InSequence(sequence).Setup(am => am.ConstraintActivityIncrement).Returns(activity);

        var jumpBackLevel = 0;
        constraintFactory.InSequence(sequence)
            .Setup(cf => cf.CreateLearnedConstraint(It.Is<ConstraintLiteral[]>(a => a.Select(l => l.StampIndex).SequenceEqual(new[] { 1, 3, 4, 7, 8 })), decisionLevel, activity, maxLBD, minLBD, out jumpBackLevel))
            .Returns(() => { jumpBackLevel = expectedJumpBackLevel; return learnedConstraint; });

        activityManager.InSequence(sequence)
            .Setup(am => am.IncreaseVariableActivity(learnedConstraint));
        activityManager.InSequence(sequence)
            .Setup(am => am.IncreaseConstraintActivity(learnedConstraint, 1));
        activityManager.InSequence(sequence)
            .Setup(am => am.DecayConstraintActivity());

        literalBlockDistanceTracker.InSequence(sequence).Setup(t => t.AddLiteralBlockDistance(lbd));

        // reset uip sense
        trail.InSequence(sequence).Setup(t => t.JumpBack(jumpBackLevel)).Callback(() => learnedConstraint.Watched1.Variable.Sense = null);

        sut.HandleConflict(conflictingConstraint);

        activityManager.VerifyAll();
        trail.VerifyAll();
        propagationRateTracker.VerifyAll();
        literalBlockDistanceTracker.VerifyAll();
        restartManager.VerifyAll();
        constraintMinimizer.VerifyAll();
        learnedConstraintCreator.VerifyAll();
        constraintFactory.VerifyAll();

        var (uip, lc) = Assert.Single(unitPropagationQueue);
        Assert.Equal(learnedConstraint.Watched1, uip);
        Assert.Equal(learnedConstraint, lc);
    }
}
