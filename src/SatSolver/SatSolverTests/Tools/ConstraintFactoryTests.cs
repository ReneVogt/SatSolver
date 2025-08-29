using Moq;
using Revo.SatSolver;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace SatSolverTests.Tools;

public sealed class ConstraintFactoryTests
{
    [Fact]
    public void Initial_SingleLiteral_WatcherConnectedOnce()
    {
        var sut = new ConstraintFactory([]);
        var variable = new Variable(17);
        var constraint = sut.CreateInitialConstraint([variable.PositiveLiteral]);

        Assert.Equal(variable.PositiveLiteral, constraint.Watched1);
        Assert.Equal(variable.PositiveLiteral, constraint.Watched2);
        Assert.Contains(constraint, variable.PositiveLiteral.Watchers);
        Assert.Single(variable.PositiveLiteral.Watchers);
        Assert.Empty(variable.NegativeLiteral.Watchers);

        Assert.Equal([variable.PositiveLiteral], constraint.Literals);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
    }
    [Fact]
    public void Initial_MultipleLiteral_WatchersConnected()
    {
        var sut = new ConstraintFactory([]);
        var v1 = new Variable(17);
        var v2 = new Variable(42);
        var v3 = new Variable(23);
        var constraint = sut.CreateInitialConstraint([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral]);

        Assert.Equal(v1.PositiveLiteral, constraint.Watched1);
        Assert.Equal(v2.NegativeLiteral, constraint.Watched2);
        
        Assert.Contains(constraint, v1.PositiveLiteral.Watchers);
        Assert.Single(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);

        Assert.Contains(constraint, v2.NegativeLiteral.Watchers);
        Assert.Single(v2.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);

        Assert.Empty(v3.PositiveLiteral.Watchers);
        Assert.Empty(v3.NegativeLiteral.Watchers);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);

        Assert.Equal([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral], constraint.Literals);
    }

    [Fact]
    public void Solution_SingleWatcher_WatcherConnectedOnce()
    {
        var sut = new ConstraintFactory([]);
        var variable = new Variable(17)
        {
            Sense = true
        };
        var variables = new[] { variable };
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var seq = new MockSequence();
        trail.InSequence(seq).Setup(t => t.Count).Returns(1);
        trail.InSequence(seq).Setup(t => t[0]).Returns(variable);
        trail.InSequence(seq).Setup(t => t.Count).Returns(1);
        var constraint = sut.CreateFromSoluution<IVariableTrail>(variables, trail.Object, 12);

        Assert.Equal(variable.NegativeLiteral, constraint.Watched1);
        Assert.Equal(variable.NegativeLiteral , constraint.Watched2);
        Assert.Contains(constraint, variable.NegativeLiteral .Watchers);
        Assert.Single(variable.NegativeLiteral.Watchers);
        Assert.Empty(variable.PositiveLiteral.Watchers);

        Assert.Equal([variable.NegativeLiteral], constraint.Literals);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(12, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.True(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
    }
    [Fact]
    public void Solution_MultipleLiteral_WatchersConnected()
    {
        var v1 = new Variable(17);
        var v2 = new Variable(42);
        var v3 = new Variable(23);
        var variables = new[] { v1, v2, v3 };
        v1.Sense = true; v2.Sense = true; v3.Sense = false;

        var sut = new ConstraintFactory([]);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var seq = new MockSequence();
        trail.InSequence(seq).Setup(t => t.Count).Returns(3);
        trail.InSequence(seq).Setup(t => t[2]).Returns(v3);
        trail.InSequence(seq).Setup(t => t.Count).Returns(3);
        trail.InSequence(seq).Setup(t => t.Count).Returns(3);
        trail.InSequence(seq).Setup(t => t[1]).Returns(v2);

        var constraint = sut.CreateFromSoluution(variables, trail.Object, 12);

        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);

        Assert.Equal(v3.PositiveLiteral, constraint.Watched1);
        Assert.Contains(constraint, v3.PositiveLiteral.Watchers);
        Assert.Single(v3.PositiveLiteral.Watchers);
        Assert.Empty(v3.NegativeLiteral.Watchers);

        Assert.Equal(v2.NegativeLiteral, constraint.Watched2);
        Assert.Contains(constraint, v2.NegativeLiteral.Watchers);
        Assert.Single(v2.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(12, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.True(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);

        Assert.Equal([v1.NegativeLiteral, v2.NegativeLiteral, v3.PositiveLiteral], constraint.Literals);
    }
    [Fact]
    public void Solution_MultipleLiteralInverse_WatchersConnected()
    {
        var v1 = new Variable(17);
        var v2 = new Variable(42);
        var v3 = new Variable(23);
        var variables = new[] { v1, v2, v3 };
        v1.Sense = true; v2.Sense = false; v3.Sense = true;

        var sut = new ConstraintFactory([]);
        var trail = new Mock<IVariableTrail>(MockBehavior.Strict);
        var seq = new MockSequence();
        trail.InSequence(seq).Setup(t => t.Count).Returns(3);
        trail.InSequence(seq).Setup(t => t[2]).Returns(v3);
        trail.InSequence(seq).Setup(t => t.Count).Returns(3);
        trail.InSequence(seq).Setup(t => t.Count).Returns(3);
        trail.InSequence(seq).Setup(t => t[1]).Returns(v2);

        var constraint = sut.CreateFromSoluution(variables, trail.Object, 12);

        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);

        Assert.Equal(v3.NegativeLiteral, constraint.Watched1);
        Assert.Contains(constraint, v3.NegativeLiteral.Watchers);
        Assert.Single(v3.NegativeLiteral.Watchers);
        Assert.Empty(v3.PositiveLiteral.Watchers);

        Assert.Equal(v2.PositiveLiteral, constraint.Watched2);
        Assert.Contains(constraint, v2.PositiveLiteral.Watchers);
        Assert.Single(v2.PositiveLiteral.Watchers);
        Assert.Empty(v2.NegativeLiteral.Watchers);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(12, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.True(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);

        Assert.Equal([v1.NegativeLiteral, v2.PositiveLiteral, v3.NegativeLiteral], constraint.Literals);
    }


    [Theory]
    [
    InlineData(2, 4),
    InlineData(3, 4),
    InlineData(0, 2)
]
    public void CreateLearned(int minimum, int maximum)
    {
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
        var learnedLiterals = new[]
        {
            variables[0].NegativeLiteral,
            variables[1].NegativeLiteral,
            variables[2].PositiveLiteral,
            variables[3].NegativeLiteral
        };

        var learnedConstraints = new List<Constraint>();
        var sut = new ConstraintFactory(learnedConstraints);

        var learnedConstraint = sut.CreateLearnedConstraint(learnedLiterals, 10, 17, maximum, minimum, out var jumpBackLevel);

        Assert.Equal(variables[1].NegativeLiteral, learnedConstraint.Watched1);
        Assert.Equal(3, learnedConstraint.LiteralBlockDistance);
        Assert.True(learnedConstraint.IsLearned);
        Assert.Equal(3 > maximum, learnedConstraint.IsOmitted);
        Assert.Equal(3 > minimum && 3 <= maximum, learnedConstraint.IsTracked);
        Assert.Equal(17, learnedConstraint.Activity);
        Assert.Equal(3, jumpBackLevel);
    }

    [Fact]
    public void CreateLearned_SingleLiteral()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[2].DecisionLevel = 3;
        variables[2].Sense = false;

        var learnedConstraints = new List<Constraint>();
        var sut = new ConstraintFactory(learnedConstraints);

        var learnedLiterals = new[] { variables[2].PositiveLiteral };
        var learnedConstraint = sut.CreateLearnedConstraint(learnedLiterals, 3, 17, 2, 4, out var jumpBackLevel);

        Assert.Equal(variables[2].PositiveLiteral, learnedConstraint.Watched1);
        Assert.Equal(1, learnedConstraint.LiteralBlockDistance);
        Assert.True(learnedConstraint.IsLearned);
        Assert.Equal(0, jumpBackLevel);
    }


    //[Fact]
    //public void Learned_SingleWatcher_NotOmitted_WatcherConnectedOnce()
    //{
    //    const double activity = 3.1416d;
    //    const int lbd = 3;

    //    var sut = new ConstraintFactory([]);
    //    var variable = new Variable(17);
    //    var constraint = sut.CreateLearnedConstraint([variable.PositiveLiteral], variable.PositiveLiteral, variable.PositiveLiteral, activity, lbd, true, false);

    //    Assert.Equal(variable.PositiveLiteral, constraint.Watched1);
    //    Assert.Equal(variable.PositiveLiteral, constraint.Watched2);
    //    Assert.Contains(constraint, variable.PositiveLiteral.Watchers);
    //    Assert.Single(variable.PositiveLiteral.Watchers);
    //    Assert.Empty(variable.NegativeLiteral.Watchers);

    //    Assert.Equal([variable.PositiveLiteral], constraint.Literals);

    //    Assert.Equal(lbd, constraint.LiteralBlockDistance);
    //    Assert.Equal(activity, constraint.Activity);
    //    Assert.True(constraint.IsTracked);
    //    Assert.True(constraint.IsLearned);
    //    Assert.False(constraint.IsOmitted);
    //}
    //[Fact]
    //public void Learned_SingleWatcher_Omitted_WatchersNotConnected()
    //{
    //    const double activity = 3.1416d;
    //    const int lbd = 3;

    //    var sut = new ConstraintFactory([]);
    //    var variable = new Variable(17);
    //    var constraint = sut.CreateLearnedConstraint([variable.PositiveLiteral], variable.PositiveLiteral, variable.PositiveLiteral, activity, lbd, false, true);

    //    Assert.Equal(variable.PositiveLiteral, constraint.Watched1);
    //    Assert.Equal(variable.PositiveLiteral, constraint.Watched2);
    //    Assert.Empty(variable.PositiveLiteral.Watchers);
    //    Assert.Empty(variable.NegativeLiteral.Watchers);

    //    Assert.Equal([variable.PositiveLiteral], constraint.Literals);

    //    Assert.Equal(lbd, constraint.LiteralBlockDistance);
    //    Assert.Equal(activity, constraint.Activity);
    //    Assert.False(constraint.IsTracked);
    //    Assert.True(constraint.IsLearned);
    //    Assert.True(constraint.IsOmitted);
    //}
    //[Fact]
    //public void Learned_MultipleLiteral_NotOmitted_WatchersConnected()
    //{
    //    const double activity = 3.1416d;
    //    const int lbd = 3;

    //    var sut = new ConstraintFactory([]);
    //    var v1 = new Variable(17);
    //    var v2 = new Variable(42);
    //    var v3 = new Variable(23);
    //    var constraint = sut.CreateLearnedConstraint([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral],
    //        v2.NegativeLiteral, v3.NegativeLiteral,
    //        activity, lbd, false, false);

    //    Assert.Empty(v1.PositiveLiteral.Watchers);
    //    Assert.Empty(v1.NegativeLiteral.Watchers);

    //    Assert.Equal(v2.NegativeLiteral, constraint.Watched1);
    //    Assert.Contains(constraint, v2.NegativeLiteral.Watchers);
    //    Assert.Single(v2.NegativeLiteral.Watchers);
    //    Assert.Empty(v2.PositiveLiteral.Watchers);

    //    Assert.Equal(v3.NegativeLiteral, constraint.Watched2);
    //    Assert.Contains(constraint, v3.NegativeLiteral.Watchers);
    //    Assert.Single(v3.NegativeLiteral.Watchers);
    //    Assert.Empty(v3.PositiveLiteral.Watchers);

    //    Assert.Equal(lbd, constraint.LiteralBlockDistance);
    //    Assert.Equal(activity, constraint.Activity);
    //    Assert.False(constraint.IsTracked);
    //    Assert.True(constraint.IsLearned);
    //    Assert.False(constraint.IsOmitted);

    //    Assert.Equal([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral], constraint.Literals);
    //}
    //[Fact]
    //public void Learned_MultipleLiteral_Omitted_WatchersNotConnected()
    //{
    //    const double activity = 3.1416d;
    //    const int lbd = 3;

    //    var sut = new ConstraintFactory([]);
    //    var v1 = new Variable(17);
    //    var v2 = new Variable(42);
    //    var v3 = new Variable(23);
    //    var constraint = sut.CreateLearnedConstraint([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral],
    //        v2.NegativeLiteral, v3.NegativeLiteral,
    //        activity, lbd, false, true);

    //    Assert.Empty(v1.PositiveLiteral.Watchers);
    //    Assert.Empty(v1.NegativeLiteral.Watchers);

    //    Assert.Equal(v2.NegativeLiteral, constraint.Watched1);
    //    Assert.Empty(v2.NegativeLiteral.Watchers);
    //    Assert.Empty(v2.PositiveLiteral.Watchers);

    //    Assert.Equal(v3.NegativeLiteral, constraint.Watched2);
    //    Assert.Empty(v3.NegativeLiteral.Watchers);
    //    Assert.Empty(v3.PositiveLiteral.Watchers);

    //    Assert.Equal(lbd, constraint.LiteralBlockDistance);
    //    Assert.Equal(activity, constraint.Activity);
    //    Assert.False(constraint.IsTracked);
    //    Assert.True(constraint.IsLearned);
    //    Assert.True(constraint.IsOmitted);

    //    Assert.Equal([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral], constraint.Literals);
    //}
    [Fact]
    public void Release_DeleteCorrectConstraints()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();
        var learnedConstraints = new List<Constraint>();
        var sut = new ConstraintFactory(learnedConstraints);

        var c01 = sut.CreateInitialConstraint([.. variables.Select(v => v.PositiveLiteral)]);
        c01.IsTracked = true;
        c01.Activity = 0;
        c01.LiteralBlockDistance = 10;

        var c02 = sut.CreateInitialConstraint([.. variables.Take(9).Select(v => v.NegativeLiteral)]);
        c02.IsTracked = true;
        c02.Activity = 0;
        c02.LiteralBlockDistance = 10;

        var c03 = sut.CreateInitialConstraint([.. variables.Take(9).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c03.IsTracked = true;
        c03.Activity = 1;
        c03.LiteralBlockDistance = 10;

        var c04 = sut.CreateInitialConstraint([.. variables.Take(9).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c04.IsTracked = true;
        c04.Activity = 2;
        c04.LiteralBlockDistance = 10;

        var c05 = sut.CreateInitialConstraint([.. variables.Take(8).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c05.IsTracked = true;
        c05.Activity = 0;
        c05.LiteralBlockDistance = 10;

        var c06 = sut.CreateInitialConstraint([.. variables.Take(10).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c06.IsTracked = true;
        c06.Activity = 0;
        c06.LiteralBlockDistance = 5;

        var c07 = sut.CreateInitialConstraint([.. variables.Take(10).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c07.IsTracked = true;
        c07.Activity = 10;
        c07.LiteralBlockDistance = 5;

        var c08 = sut.CreateInitialConstraint([.. variables.Skip(3).Take(4).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]); ;
        c08.IsTracked = true;
        c08.Activity = 10;
        c08.LiteralBlockDistance = 5;

        var c09 = sut.CreateInitialConstraint([.. variables.Skip(5).Take(2).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c09.IsTracked = true;
        c09.Activity = 20;
        c09.LiteralBlockDistance = 3;

        learnedConstraints.AddRange(c03, c02, c07, c06, c08, c01, c04, c09, c05);
        var expectedDeleted = new[] { c01, c02, c03, c04, c05, c06 };
        var expectedKept = new[] { c09, c08, c07 };

        learnedConstraints.Sort((left, right) =>
            (left.LiteralBlockDistance, -left.Activity, left.Literals.Length)
            .CompareTo((right.LiteralBlockDistance, -right.Activity, right.Literals.Length)));

        sut.ReleaseLearnedConstraints(0.6);

        Assert.Equal(expectedKept, learnedConstraints);
        var allWatched = variables.SelectMany(v => v.PositiveLiteral.Watchers.Concat(v.NegativeLiteral.Watchers)).ToHashSet();
        Assert.All(expectedDeleted, deletedConstraint =>
        {
            Assert.False(deletedConstraint.IsTracked);
            Assert.DoesNotContain(deletedConstraint, allWatched);
        });
    }
}