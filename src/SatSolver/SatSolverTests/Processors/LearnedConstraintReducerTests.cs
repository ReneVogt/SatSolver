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
        var learnedConstraints = new List<Constraint>();
        var constraintFactory = new ConstraintFactory(learnedConstraints);

        var c01 = constraintFactory.CreateInitialConstraint([.. variables.Select(v => v.PositiveLiteral)]);
        c01.IsTracked = true;
        c01.Activity = 0;
        c01.LiteralBlockDistance = 10;

        var c02 = constraintFactory.CreateInitialConstraint([.. variables.Take(9).Select(v => v.NegativeLiteral)]);
        c02.IsTracked = true;
        c02.Activity = 0;
        c02.LiteralBlockDistance = 10;

        var c03 = constraintFactory.CreateInitialConstraint([.. variables.Take(9).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c03.IsTracked = true;
        c03.Activity = 1;
        c03.LiteralBlockDistance = 10;

        var c04 = constraintFactory.CreateInitialConstraint([.. variables.Take(9).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c04.IsTracked = true;
        c04.Activity = 2;
        c04.LiteralBlockDistance = 10;

        var c05 = constraintFactory.CreateInitialConstraint([.. variables.Take(8).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c05.IsTracked = true;
        c05.Activity = 0;
        c05.LiteralBlockDistance = 10;

        var c06 = constraintFactory.CreateInitialConstraint([.. variables.Take(10).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c06.IsTracked = true;
        c06.Activity = 0;
        c06.LiteralBlockDistance = 5;

        var c07 = constraintFactory.CreateInitialConstraint([.. variables.Take(10).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c07.IsTracked = true;
        c07.Activity = 10;
        c07.LiteralBlockDistance = 5;

        var c08 = constraintFactory.CreateInitialConstraint([.. variables.Skip(3).Take(4).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c08.IsTracked = true;
        c08.Activity = 10;
        c08.LiteralBlockDistance = 5;

        var c09 = constraintFactory.CreateInitialConstraint([.. variables.Skip(5).Take(2).Select(v => (v.Index & 1) == 1 ? v.NegativeLiteral : v.PositiveLiteral)]);
        c09.IsTracked = true;
        c09.Activity = 20;
        c09.LiteralBlockDistance = 3;

        learnedConstraints.AddRange(c03, c02, c07, c06, c08, c01, c04, c09, c05);
        var expectedDeleted = new[] { c01, c02, c03, c04, c05, c06 };
        var expectedKept = new[] { c09, c08, c07 };

        var options = new SatSolverOptions
        {
            ConstraintDeletion = new()
            {
                RatioToDelete = 0.6
            }
        };
        var sut = new LearnedConstraintsReducer<IConstraintFactory>(
            options, 
            learnedConstraints, 
            12, constraintFactory);

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

        var constraintFactory = new ConstraintFactory([]);
        var c = constraintFactory.CreateInitialConstraint([.. variables.Select(v => v.PositiveLiteral)]);
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
        var sut = new LearnedConstraintsReducer<IConstraintFactory>(
            options,
            learnedConstraints,
            10, null!);

        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(100, learnedConstraints.Count);
    }
    [Fact]
    public void ReduceIfNecessary_ByCount_False()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();

        var constraintFactory = new ConstraintFactory([]);
        var c = constraintFactory.CreateInitialConstraint([..variables.Select(v => v.PositiveLiteral)]);
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
        var sut = new LearnedConstraintsReducer<IConstraintFactory>(
            options,
            learnedConstraints,
            12, null!);

        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(100, learnedConstraints.Count);
    }
    [Fact]
    public void ReduceIfNecessary_ByCount_True()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();

        var learnedConstraints = new List<Constraint>();
        var constraintFactory = new ConstraintFactory(learnedConstraints);
        var c = constraintFactory.CreateInitialConstraint([.. variables.Select(v => v.PositiveLiteral)]);
        learnedConstraints.AddRange(Enumerable.Repeat(c, 100));

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
        var sut = new LearnedConstraintsReducer<IConstraintFactory>(
            options,
            learnedConstraints,
            11, constraintFactory);

        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(50, learnedConstraints.Count);
    }
    [Fact]
    public void ReduceIfNecessary_ByConflictCount_False()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();

        var constraintFactory = new ConstraintFactory([]);
        var c = constraintFactory.CreateInitialConstraint([..variables.Select(v => v.PositiveLiteral)]);
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
        var sut = new LearnedConstraintsReducer<IConstraintFactory>(
            options,
            learnedConstraints,
            11, null!);

        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(100, learnedConstraints.Count);
    }
    [Fact]
    public void ReduceIfNecessary_ByConflictInterval_True()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();
        var c = new Constraint([.. variables.Select(v => v.PositiveLiteral)], variables[0].PositiveLiteral, variables[1].PositiveLiteral);
        var learnedConstraints = Enumerable.Repeat(c, 100).ToList();
        var constraintFactory = new ConstraintFactory(learnedConstraints);

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
        var sut = new LearnedConstraintsReducer<IConstraintFactory>(
            options,
            learnedConstraints,
            11, constraintFactory);

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

        var constraintFactory = new ConstraintFactory([]);
        var c = constraintFactory.CreateInitialConstraint([..variables.Select(v => v.PositiveLiteral)]);
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
        var sut = new LearnedConstraintsReducer<IConstraintFactory>(
            options,
            learnedConstraints,
            11, null!);

        sut.ReduceLearnedConstraintsIfNecessary();
        Assert.Equal(100, learnedConstraints.Count);
    }
}
