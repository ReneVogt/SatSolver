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
        var sut = new ConstraintFactory([], []);
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
    public void Initial_Binary_BinariesConnected()
    {
        var sut = new ConstraintFactory([], []);
        var v1 = new Variable(0);
        var v2 = new Variable(1);
        var constraint = sut.CreateInitialConstraint([v1.PositiveLiteral, v2.NegativeLiteral]);

        Assert.Equal(v1.PositiveLiteral, constraint.Watched1);
        Assert.Equal(v2.NegativeLiteral, constraint.Watched2);
        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);
        Assert.Empty(v2.NegativeLiteral.Watchers);

        Assert.Equal((v2.NegativeLiteral, constraint), Assert.Single(v1.PositiveLiteral.Binaries));
        Assert.Equal((v1.PositiveLiteral, constraint), Assert.Single(v2.NegativeLiteral.Binaries));

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
    }
    [Fact]
    public void Initial_MultipleLiteral_WatchersConnected()
    {
        var sut = new ConstraintFactory([], []);
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
    public void Addidtional_SingleWatcher_WatcherConnectedOnce()
    {
        var sut = new ConstraintFactory([], []);
        var variable = new Variable(17)
        {
            Sense = true
        };
        var constraint = sut.CreateAdditionalConstraint([variable.NegativeLiteral]);

        Assert.Equal(variable.NegativeLiteral, constraint.Watched1);
        Assert.Equal(variable.NegativeLiteral, constraint.Watched2);
        Assert.Contains(constraint, variable.NegativeLiteral.Watchers);
        Assert.Single(variable.NegativeLiteral.Watchers);
        Assert.Empty(variable.PositiveLiteral.Watchers);

        Assert.Equal([variable.NegativeLiteral], constraint.Literals);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
        Assert.True(constraint.IsAdditional);
    }
    [Fact]
    public void Additional_Binary_Unassigned()
    {
        var sut = new ConstraintFactory([], []);
        var v1 = new Variable(0);
        var v2 = new Variable(1);
        var constraint = sut.CreateAdditionalConstraint([v1.PositiveLiteral, v2.NegativeLiteral]);

        Assert.Equal(v1.PositiveLiteral, constraint.Watched1);
        Assert.Equal(v2.NegativeLiteral, constraint.Watched2);
        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);
        Assert.Empty(v2.NegativeLiteral.Watchers);

        Assert.Equal((v2.NegativeLiteral, constraint), Assert.Single(v1.PositiveLiteral.Binaries));
        Assert.Equal((v1.PositiveLiteral, constraint), Assert.Single(v2.NegativeLiteral.Binaries));

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
        Assert.True(constraint.IsAdditional);
    }
    [Fact]
    public void Additional_Binary_OneTrue()
    {
        var sut = new ConstraintFactory([], []);
        var v1 = new Variable(0);
        var v2 = new Variable(1);
        v1.Sense = false;
        v1.DecisionLevel = 25;
        v2.Sense = false;
        v2.DecisionLevel = 10;

        var constraint = sut.CreateAdditionalConstraint([v1.PositiveLiteral, v2.NegativeLiteral]);

        Assert.Equal(v2.NegativeLiteral, constraint.Watched1);
        Assert.Equal(v1.PositiveLiteral, constraint.Watched2);
        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);
        Assert.Empty(v2.NegativeLiteral.Watchers);

        Assert.Equal((v2.NegativeLiteral, constraint), Assert.Single(v1.PositiveLiteral.Binaries));
        Assert.Equal((v1.PositiveLiteral, constraint), Assert.Single(v2.NegativeLiteral.Binaries));

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
        Assert.True(constraint.IsAdditional);
    }
    [Fact]
    public void Additional_Binary_AllFalse()
    {
        var sut = new ConstraintFactory([], []);
        var v1 = new Variable(0);
        var v2 = new Variable(1);
        v1.Sense = false;
        v1.DecisionLevel = 10;
        v2.Sense = true;
        v2.DecisionLevel = 25;

        var constraint = sut.CreateAdditionalConstraint([v1.PositiveLiteral, v2.NegativeLiteral]);

        Assert.Equal(v2.NegativeLiteral, constraint.Watched1);
        Assert.Equal(v1.PositiveLiteral, constraint.Watched2);
        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);
        Assert.Empty(v2.NegativeLiteral.Watchers);

        Assert.Equal((v2.NegativeLiteral, constraint), Assert.Single(v1.PositiveLiteral.Binaries));
        Assert.Equal((v1.PositiveLiteral, constraint), Assert.Single(v2.NegativeLiteral.Binaries));

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
        Assert.True(constraint.IsAdditional);
    }
    [Fact]
    public void Addidtional_AllAssigned_OneTrue()
    {
        var sut = new ConstraintFactory([], []);
        var v0 = new Variable(0) { Sense = true, DecisionLevel = 8 };
        var v1 = new Variable(1) { Sense = true, DecisionLevel = 7 };
        var v2 = new Variable(2) { Sense = true, DecisionLevel = 1 };
        var v3 = new Variable(3) { Sense = true, DecisionLevel = 6 };

        var clause = new[] { v0.NegativeLiteral, v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral };
        var constraint = sut.CreateAdditionalConstraint(clause);

        Assert.Equal(v1.PositiveLiteral, constraint.Watched1);
        Assert.Equal(v3.NegativeLiteral, constraint.Watched2);
        Assert.Equal(constraint, Assert.Single(constraint.Watched1.Watchers));
        Assert.Equal(constraint, Assert.Single(constraint.Watched2.Watchers));

        Assert.Empty(v0.PositiveLiteral.Watchers);
        Assert.Empty(v0.NegativeLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);
        Assert.Empty(v2.NegativeLiteral.Watchers);
        Assert.Empty(v3.PositiveLiteral.Watchers);

        Assert.Equal(clause, constraint.Literals);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
        Assert.True(constraint.IsAdditional);
    }
    [Fact]
    public void Addidtional_AllAssigned_TwoTrue()
    {
        var sut = new ConstraintFactory([], []);
        var v0 = new Variable(0) { Sense = true, DecisionLevel = 8 };
        var v1 = new Variable(1) { Sense = true, DecisionLevel = 9 };
        var v2 = new Variable(2) { Sense = true, DecisionLevel = 6 };
        var v3 = new Variable(3) { Sense = true, DecisionLevel = 7 };

        var clause = new[] { v0.PositiveLiteral, v1.NegativeLiteral, v2.NegativeLiteral, v3.PositiveLiteral };
        var constraint = sut.CreateAdditionalConstraint(clause);

        Assert.Equal(v3.PositiveLiteral, constraint.Watched1);
        Assert.Equal(v2.NegativeLiteral, constraint.Watched2);
        Assert.Equal(constraint, Assert.Single(constraint.Watched1.Watchers));
        Assert.Equal(constraint, Assert.Single(constraint.Watched2.Watchers));

        Assert.Empty(v0.PositiveLiteral.Watchers);
        Assert.Empty(v0.NegativeLiteral.Watchers);
        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);
        Assert.Empty(v3.NegativeLiteral.Watchers);

        Assert.Equal(clause, constraint.Literals);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
        Assert.True(constraint.IsAdditional);
    }
    [Fact]
    public void Addidtional_AllAssigned_NoTrue()
    {
        var sut = new ConstraintFactory([], []);
        var v0 = new Variable(0) { Sense = true, DecisionLevel = 8 };
        var v1 = new Variable(1) { Sense = true, DecisionLevel = 9 };
        var v2 = new Variable(2) { Sense = true, DecisionLevel = 6 };
        var v3 = new Variable(3) { Sense = true, DecisionLevel = 7 };

        var clause = new[] { v0.NegativeLiteral, v1.NegativeLiteral, v2.NegativeLiteral, v3.NegativeLiteral};
        var constraint = sut.CreateAdditionalConstraint(clause);

        Assert.Equal(v1.NegativeLiteral, constraint.Watched1);
        Assert.Equal(v0.NegativeLiteral, constraint.Watched2);
        Assert.Equal(constraint, Assert.Single(constraint.Watched1.Watchers));
        Assert.Equal(constraint, Assert.Single(constraint.Watched2.Watchers));

        Assert.Empty(v0.PositiveLiteral.Watchers);
        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);
        Assert.Empty(v2.NegativeLiteral.Watchers);
        Assert.Empty(v3.PositiveLiteral .Watchers);
        Assert.Empty(v3.NegativeLiteral.Watchers);

        Assert.Equal(clause, constraint.Literals);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
        Assert.True(constraint.IsAdditional);
    }
    [Fact]
    public void Addidtional_AllUnassigned()
    {
        var sut = new ConstraintFactory([], []);
        var v0 = new Variable(0) { Sense = null, DecisionLevel = 0 };
        var v1 = new Variable(1) { Sense = null, DecisionLevel = 0 };
        var v2 = new Variable(2) { Sense = null, DecisionLevel = 0 };
        var v3 = new Variable(3) { Sense = null, DecisionLevel = 0 };

        var clause = new[] { v0.PositiveLiteral, v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral };
        var constraint = sut.CreateAdditionalConstraint(clause);

        Assert.Equal(v0.PositiveLiteral, constraint.Watched1);
        Assert.Equal(v1.PositiveLiteral, constraint.Watched2);
        Assert.Equal(constraint, Assert.Single(constraint.Watched1.Watchers));
        Assert.Equal(constraint, Assert.Single(constraint.Watched2.Watchers));

        Assert.Empty(v0.NegativeLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);
        Assert.Empty(v2.NegativeLiteral.Watchers);
        Assert.Empty(v3.PositiveLiteral.Watchers);
        Assert.Empty(v3.NegativeLiteral.Watchers);

        Assert.Equal(clause, constraint.Literals);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
        Assert.True(constraint.IsAdditional);
    }
    [Fact]
    public void Addidtional_HalfAssigned_NoTrue()
    {
        var sut = new ConstraintFactory([], []);
        var v0 = new Variable(0) { Sense = true, DecisionLevel = 8 };
        var v1 = new Variable(1) { Sense = null, DecisionLevel = 0 };
        var v2 = new Variable(2) { Sense = false, DecisionLevel = 6 };
        var v3 = new Variable(3) { Sense = null, DecisionLevel = 0 };

        var clause = new[] { v0.NegativeLiteral, v1.PositiveLiteral, v2.PositiveLiteral, v3.NegativeLiteral };
        var constraint = sut.CreateAdditionalConstraint(clause);

        Assert.Equal(v1.PositiveLiteral, constraint.Watched1);
        Assert.Equal(v3.NegativeLiteral, constraint.Watched2);
        Assert.Equal(constraint, Assert.Single(constraint.Watched1.Watchers));
        Assert.Equal(constraint, Assert.Single(constraint.Watched2.Watchers));

        Assert.Empty(v0.PositiveLiteral.Watchers);
        Assert.Empty(v0.NegativeLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);
        Assert.Empty(v2.NegativeLiteral.Watchers);
        Assert.Empty(v3.PositiveLiteral.Watchers);

        Assert.Equal(clause, constraint.Literals);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
        Assert.True(constraint.IsAdditional);
    }
    [Fact]
    public void Addidtional_HalfAssigned_OneTrue()
    {
        var sut = new ConstraintFactory([], []);
        var v0 = new Variable(0) { Sense = true, DecisionLevel = 8 };
        var v1 = new Variable(1) { Sense = null, DecisionLevel = 0 };
        var v2 = new Variable(2) { Sense = false, DecisionLevel = 10 };
        var v3 = new Variable(3) { Sense = null, DecisionLevel = 0 };
        var v4 = new Variable(4) { Sense = true, DecisionLevel = 7 };

        var clause = new[] { v0.NegativeLiteral, v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral, v4.NegativeLiteral };
        var constraint = sut.CreateAdditionalConstraint(clause);

        Assert.Equal(v2.NegativeLiteral, constraint.Watched1);
        Assert.Equal(v0.NegativeLiteral, constraint.Watched2);
        Assert.Equal(constraint, Assert.Single(constraint.Watched1.Watchers));
        Assert.Equal(constraint, Assert.Single(constraint.Watched2.Watchers));

        Assert.Empty(v0.PositiveLiteral.Watchers);
        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);
        Assert.Empty(v3.PositiveLiteral.Watchers);
        Assert.Empty(v3.NegativeLiteral.Watchers);
        Assert.Empty(v4.PositiveLiteral.Watchers);
        Assert.Empty(v4.NegativeLiteral.Watchers);

        Assert.Equal(clause, constraint.Literals);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
        Assert.True(constraint.IsAdditional);
    }
    [Fact]
    public void Addidtional_HalfAssigned_TwoTrue()
    {
        var sut = new ConstraintFactory([], []);
        var v0 = new Variable(0) { Sense = true, DecisionLevel = 8 };
        var v1 = new Variable(1) { Sense = null, DecisionLevel = 0 };
        var v2 = new Variable(2) { Sense = false, DecisionLevel = 10 };
        var v3 = new Variable(3) { Sense = null, DecisionLevel = 0 };
        var v4 = new Variable(4) { Sense = true, DecisionLevel = 7 };

        var clause = new[] { v0.PositiveLiteral, v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral, v4.NegativeLiteral };
        var constraint = sut.CreateAdditionalConstraint(clause);

        Assert.Equal(v0.PositiveLiteral, constraint.Watched1);
        Assert.Equal(v4.NegativeLiteral, constraint.Watched2);
        Assert.Equal(constraint, Assert.Single(constraint.Watched1.Watchers));
        Assert.Equal(constraint, Assert.Single(constraint.Watched2.Watchers));

        Assert.Empty(v0.NegativeLiteral.Watchers);
        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);
        Assert.Empty(v2.NegativeLiteral.Watchers);
        Assert.Empty(v3.PositiveLiteral.Watchers);
        Assert.Empty(v3.NegativeLiteral.Watchers);
        Assert.Empty(v4.PositiveLiteral.Watchers);

        Assert.Equal(clause, constraint.Literals);

        Assert.Equal(0, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.False(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
        Assert.True(constraint.IsAdditional);
    }
    [Fact]
    public void Addidtional_ForCoverage_1()
    {
        var sut = new ConstraintFactory([], []);
        var v0 = new Variable(0) { Sense = false, DecisionLevel = 3 };
        var v1 = new Variable(1) { Sense = false, DecisionLevel = 1 };
        var v2 = new Variable(1) { Sense = false, DecisionLevel = 2 };

        var clause = new[] { v0.PositiveLiteral, v1.PositiveLiteral, v2.PositiveLiteral };
        var constraint = sut.CreateAdditionalConstraint(clause);

        Assert.Equal(v0.PositiveLiteral, constraint.Watched1);
        Assert.Equal(v2.PositiveLiteral, constraint.Watched2);
    }
    [Fact]
    public void Addidtional_ForCoverage_2()
    {
        var sut = new ConstraintFactory([], []);
        var v0 = new Variable(0) { Sense = null, DecisionLevel = 0 };
        var v1 = new Variable(1) { Sense = false, DecisionLevel = 1 };
        var v2 = new Variable(1) { Sense = false, DecisionLevel = 2 };

        var clause = new[] { v0.PositiveLiteral, v1.PositiveLiteral, v2.PositiveLiteral };
        var constraint = sut.CreateAdditionalConstraint(clause);

        Assert.Equal(v0.PositiveLiteral, constraint.Watched1);
        Assert.Equal(v2.PositiveLiteral, constraint.Watched2);
    }

    [Theory]
    [
        InlineData(2, 4),
        InlineData(3, 4),
        InlineData(0, 2)
    ]
    public void Learned(int minimum, int maximum)
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
        var sut = new ConstraintFactory([], learnedConstraints);

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
    public void Learned_SingleLiteral()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[2].DecisionLevel = 3;
        variables[2].Sense = false;

        var learnedConstraints = new List<Constraint>();
        var sut = new ConstraintFactory([], learnedConstraints);

        var learnedLiterals = new[] { variables[2].PositiveLiteral };
        var learnedConstraint = sut.CreateLearnedConstraint(learnedLiterals, 3, 17, 2, 4, out var jumpBackLevel);

        Assert.Equal(variables[2].PositiveLiteral, learnedConstraint.Watched1);
        Assert.Equal(1, learnedConstraint.LiteralBlockDistance);
        Assert.True(learnedConstraint.IsLearned);
        Assert.Equal(0, jumpBackLevel);
    }
    [Fact]
    public void Learned_Binary_BinariesConnected()
    {
        var sut = new ConstraintFactory([], []);
        var v1 = new Variable(0);
        var v2 = new Variable(1);
        v1.Sense = false;
        v1.DecisionLevel = 12;
        v2.Sense = true;
        v2.DecisionLevel = 14;

        var constraint = sut.CreateLearnedConstraint([v1.PositiveLiteral, v2.NegativeLiteral], 14, 17, 2, 2, out var jumpBackLevel);

        Assert.Equal(12, jumpBackLevel);

        Assert.Equal(v2.NegativeLiteral, constraint.Watched1);
        Assert.Equal(v1.PositiveLiteral, constraint.Watched2);
        Assert.Empty(v1.PositiveLiteral.Watchers);
        Assert.Empty(v1.NegativeLiteral.Watchers);
        Assert.Empty(v2.PositiveLiteral.Watchers);
        Assert.Empty(v2.NegativeLiteral.Watchers);

        Assert.Equal((v2.NegativeLiteral, constraint), Assert.Single(v1.PositiveLiteral.Binaries));
        Assert.Equal((v1.PositiveLiteral, constraint), Assert.Single(v2.NegativeLiteral.Binaries));

        Assert.Equal(2, constraint.LiteralBlockDistance);
        Assert.Equal(0, constraint.Activity);
        Assert.False(constraint.IsTracked);
        Assert.True(constraint.IsLearned);
        Assert.False(constraint.IsOmitted);
    }


    [Fact]
    public void ReleaseLearnedConstraints_DeleteCorrectConstraints()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();
        var learnedConstraints = new List<Constraint>();
        var sut = new ConstraintFactory([], learnedConstraints);

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

    [Fact]
    public void ReleaseConstraint_WatchersDisconnected()
    {
        var variables = Enumerable.Range(0, 2).Select(i => new Variable(i)).ToArray();
        var constraint = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral],
                variables[0].PositiveLiteral, variables[1].PositiveLiteral);
        constraint.Watched1.Watchers.Add(constraint);
        constraint.Watched2.Watchers.Add(constraint);

        var sut = new ConstraintFactory([], []);
        sut.ReleaseConstraint(constraint);

        Assert.Empty(variables[0].PositiveLiteral.Watchers);
        Assert.Empty(variables[1].PositiveLiteral.Watchers);
    }
    [Fact]
    public void ReleaseConstraint_IgnoreOmitted()
    {
        var variables = Enumerable.Range(0, 2).Select(i => new Variable(i)).ToArray();
        var constraint = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral],
                variables[0].PositiveLiteral, variables[1].PositiveLiteral);
        constraint.Watched1.Watchers.Add(constraint);
        constraint.Watched2.Watchers.Add(constraint);

        constraint.IsOmitted = true;

        var sut = new ConstraintFactory([], []);
        sut.ReleaseConstraint(constraint);

        Assert.Equal(constraint, Assert.Single(variables[0].PositiveLiteral.Watchers));
        Assert.Equal(constraint, Assert.Single(variables[1].PositiveLiteral.Watchers));
    }

    [Fact]
    public void ReleaseAdditionalConstraints_Released()
    {
        var store = new TestComponentStore(new(), 3, null!);
        var variables = store.Variables;
        var literals = store.Literals;

        var learned = new Constraint([literals[0], literals[2], literals[4]],
            literals[0],
            literals[2]);
        learned.Watched1.Watchers.Add(learned);
        learned.Watched2.Watchers.Add(learned);
        learned.IsLearned = true;
        var learnedBinary = new Constraint([literals[0], literals[2]],
            literals[0],
            literals[2]);
        literals[0].Binaries.Add((literals[2], learnedBinary));
        literals[2].Binaries.Add((literals[0], learnedBinary));
        learnedBinary.IsLearned = true;

        var untracked = new Constraint([literals[0], literals[2], literals[4]],
            literals[0],
            literals[2]);
        untracked.Watched1.Watchers.Add(untracked);
        untracked.Watched2.Watchers.Add(untracked);
        untracked.IsLearned = true;
        var untrackedBinary = new Constraint([literals[0], literals[2]],
            literals[0],
            literals[2]);
        literals[0].Binaries.Add((literals[2], untrackedBinary));
        literals[2].Binaries.Add((literals[0], untrackedBinary));
        untrackedBinary.IsLearned = true;

        var additional = new Constraint([literals[1], literals[3], literals[5]],
            literals[1],
            literals[3]);
        additional.Watched1.Watchers.Add(additional);
        additional.Watched2.Watchers.Add(additional);
        additional.IsAdditional = true;
        var additionalBinary = new Constraint([literals[1], literals[3]],
            literals[1],
            literals[3]);
        literals[1].Binaries.Add((literals[3], additionalBinary));
        literals[3].Binaries.Add((literals[1], additionalBinary));
        additionalBinary.IsAdditional = true;

        var stable = new Constraint([literals[0], literals[3], literals[4]],
            literals[0],
            literals[3]);
        stable.Watched1.Watchers.Add(stable);
        stable.Watched2.Watchers.Add(stable);
        var stableBinary = new Constraint([literals[0], literals[3]],
            literals[0],
            literals[3]);
        literals[0].Binaries.Add((literals[3], stableBinary));
        literals[3].Binaries.Add((literals[0], stableBinary));

        var learnedConstraints = new List<Constraint> { learned, learnedBinary }; // not the untracked!
        var sut = new ConstraintFactory(store.Literals, learnedConstraints);
        sut.ReleaseAdditionalConstraints();

        Assert.Equal(stable, Assert.Single(literals[0].Watchers));
        Assert.Equal((literals[3], stableBinary), Assert.Single(literals[0].Binaries));
        Assert.Empty(literals[1].Watchers);
        Assert.Empty(literals[1].Binaries);
        Assert.Empty(literals[2].Watchers);
        Assert.Empty(literals[2].Binaries);
        Assert.Equal(stable, Assert.Single(literals[3].Watchers));
        Assert.Equal((literals[0], stableBinary), Assert.Single(literals[3].Binaries));
        Assert.Empty(literals[4].Watchers);
        Assert.Empty(literals[4].Binaries);
        Assert.Empty(literals[5].Watchers);
        Assert.Empty(literals[5].Binaries);
        Assert.Empty(learnedConstraints);
    }

}