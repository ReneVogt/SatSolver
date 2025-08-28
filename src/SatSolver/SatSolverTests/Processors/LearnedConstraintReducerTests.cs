using Moq;
using Revo.SatSolver;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace SatSolverTests.Processors;

public sealed class LearnedConstraintReducerTests
{
    [Fact]
    public void Reduce_DeleteCorrectConstraints()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();

        var c01 = new Constraint(variables.Select(v => v.PositiveLiteral))
        {
            IsTracked = true,
            Activity = 0,
            LiteralBlockDistance = 10
        };
        var c02 = new Constraint(variables.Take(9).Select(v => v.NegativeLiteral))
        {
            IsTracked = true,
            Activity = 0,
            LiteralBlockDistance = 10
        };
        var c03 = new Constraint(variables.Take(9).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral))
        {
            IsTracked = true,
            Activity = 1,
            LiteralBlockDistance = 10
        };
        var c04 = new Constraint(variables.Take(9).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral))
        {
            IsTracked = true,
            Activity = 2,
            LiteralBlockDistance = 10
        };
        var c05 = new Constraint(variables.Take(8).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral))
        {
            IsTracked = true,
            Activity = 0,
            LiteralBlockDistance = 10
        };
        var c06 = new Constraint(variables.Take(10).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral))
        {
            IsTracked = true,
            Activity = 0,
            LiteralBlockDistance = 5
        };
        var c07 = new Constraint(variables.Take(10).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral))
        {
            IsTracked = true,
            Activity = 10,
            LiteralBlockDistance = 5
        };
        var c08 = new Constraint(variables.Skip(3).Take(4).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral))
        {
            IsTracked = true,
            Activity = 10,
            LiteralBlockDistance = 5
        };
        var c09 = new Constraint(variables.Skip(5).Take(2).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral))
        {
            IsTracked = true,
            Activity = 20,
            LiteralBlockDistance = 3
        };

        var learnedConstraints = new List<Constraint> { c03, c02, c07, c06, c08, c01, c04, c09, c05 };
        var expectedDeleted = new[] { c01, c02, c03, c04, c05, c06 };
        var expectedKept = new[] { c09, c08, c07 };

        var options = new SatSolverOptions
        {
            ConstraintDeletion = new()
            {
                RatioToDelete = 0.6
            }
        };
        var sut = new LearnedConstraintsReducer(
            options, 
            learnedConstraints, 
            12);

        sut.ReduceLearnedConstraints();

        Assert.Equal(expectedKept, learnedConstraints);
        var allWatched = variables.SelectMany(v => v.PositiveLiteral.Watchers.Concat(v.NegativeLiteral.Watchers)).ToHashSet();
        Assert.All(expectedDeleted, deletedConstraint =>
        {
            Assert.False(deletedConstraint.IsTracked);
            Assert.DoesNotContain(deletedConstraint, allWatched);
        });
    }

    [Fact]
    public void ReduceIfNecessary_DontReduce()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();

        var c = new Constraint(variables.Select(v => v.PositiveLiteral));
        var learnedConstraints = Enumerable.Repeat(c, 100).ToList();

        var options = new SatSolverOptions
        {
            ConstraintDeletion = new()
            {
                RatioToDelete = 0.5,
                ReduceOnRestart = false,
                ConflictInterval = null,
                OriginalConstraintCountFactor = null
            }
        };
        var sut = new LearnedConstraintsReducer(
            options,
            learnedConstraints,
            10);

        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(100, learnedConstraints.Count);
    }
    [Fact]
    public void ReduceIfNecessary_ByCount_False()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();

        var c = new Constraint(variables.Select(v => v.PositiveLiteral));
        var learnedConstraints = Enumerable.Repeat(c, 100).ToList();

        var options = new SatSolverOptions
        {
            ConstraintDeletion = new()
            {
                RatioToDelete = 0.5,
                ReduceOnRestart = false,
                ConflictInterval = null,
                OriginalConstraintCountFactor = 9
            }
        };
        var sut = new LearnedConstraintsReducer(
            options,
            learnedConstraints,
            12);

        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(100, learnedConstraints.Count);
    }
    [Fact]
    public void ReduceIfNecessary_ByCount_True()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();

        var c = new Constraint(variables.Select(v => v.PositiveLiteral));
        var learnedConstraints = Enumerable.Repeat(c, 100).ToList();

        var options = new SatSolverOptions
        {
            ConstraintDeletion = new()
            {
                RatioToDelete = 0.5,
                ReduceOnRestart = false,
                ConflictInterval = null,
                OriginalConstraintCountFactor = 9
            }
        };
        var sut = new LearnedConstraintsReducer(
            options,
            learnedConstraints,
            11);

        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(50, learnedConstraints.Count);
    }
    [Fact]
    public void ReduceIfNecessary_ByConflictCount_False()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();

        var c = new Constraint(variables.Select(v => v.PositiveLiteral));
        var learnedConstraints = Enumerable.Repeat(c, 100).ToList();

        var options = new SatSolverOptions
        {
            ConstraintDeletion = new()
            {
                RatioToDelete = 0.5,
                ReduceOnRestart = false,
                ConflictInterval = 1000,
                OriginalConstraintCountFactor = null
            }
        };
        var sut = new LearnedConstraintsReducer(
            options,
            learnedConstraints,
            11);

        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(100, learnedConstraints.Count);
    }
    [Fact]
    public void ReduceIfNecessary_ByConflictInterval_True()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();

        var c = new Constraint(variables.Select(v => v.PositiveLiteral));
        var learnedConstraints = Enumerable.Repeat(c, 100).ToList();

        var options = new SatSolverOptions
        {
            ConstraintDeletion = new()
            {
                RatioToDelete = 0.5,
                ReduceOnRestart = false,
                ConflictInterval = 2,
                OriginalConstraintCountFactor = null
            }
        };
        var sut = new LearnedConstraintsReducer(
            options,
            learnedConstraints,
            11);

        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(100, learnedConstraints.Count);
        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(50, learnedConstraints.Count);
        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(50, learnedConstraints.Count);
    }
    [Fact]
    public void ReduceIfNecessary_NoRatio_False()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();

        var c = new Constraint(variables.Select(v => v.PositiveLiteral));
        var learnedConstraints = Enumerable.Repeat(c, 100).ToList();

        var options = new SatSolverOptions
        {
            ConstraintDeletion = new()
            {
                RatioToDelete = 0,
                ReduceOnRestart = false,
                ConflictInterval = 0,
                OriginalConstraintCountFactor = 9
            }
        };
        var sut = new LearnedConstraintsReducer(
            options,
            learnedConstraints,
            11);

        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(100, learnedConstraints.Count);
    }
}
