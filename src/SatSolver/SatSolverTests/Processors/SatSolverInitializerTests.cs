using Revo.SatSolver;
using Revo.SatSolver.Parsing;
using Revo.SatSolver.Processors;

namespace SatSolverTests.Processors;

public sealed class SatSolverInitializerTests
{
    [Fact]
    public void ThrowsOnInvalidVariableActivityDecayFactor()
    {
        var problem = new Problem(2, [new Clause([1, 2])]);
        var options = new SatSolverOptions
        {
            VariableActivityDecayFactor = 0,
        };
        Assert.Throws<ArgumentException>(() => new SatSolverInitializer(problem, options, default));
    }
    [Fact]
    public void ThrowsOnInvalidConstraintActivityDecayFactor()
    {
        var problem = new Problem(2, [new Clause([1, 2])]);
        var options = new SatSolverOptions
        {
            ConstraintActivityDecayFactor = 0,
        };
        Assert.Throws<ArgumentException>(() => new SatSolverInitializer(problem, options, default));
    }

    [Fact]
    public void CalculateCorrectActivitiesAndPolarities()
    {
        var options = new SatSolverOptions
        {
            Restart = new() // for coverage
            {
                Luby = false,
                Interval = null
            }
        };
        var problem = new Problem(
            10,
            [
                new ([1, 2]),
                new ([-1, 3, 1, 4]),
                new ([5, -5]),
                new ([-1, -3, 6]),
                new ([-7, 2, -8]),
                new ([-8]),
                new ([9])
            ]);

        var sut = new SatSolverInitializer(problem, options, default);
        var store = sut.Initialize();

        // all variables, even obsolete
        Assert.Equal(10, store.Variables.Length);
        // two tautologies
        Assert.Equal(problem.Clauses.Length-2, store.OriginalConstraintCount);

        // unit propagations
        Assert.Equal([store.Variables[7].NegativeLiteral, store.Variables[8].PositiveLiteral], store.UnitPropagationQueue.Select(u => u.UnitLiteral).OrderBy(l => l.Variable.Index));

        // generated constraints
        var constraints = store.Variables.SelectMany(v => v.PositiveLiteral.Watchers.Concat(v.NegativeLiteral.Watchers))
            .Distinct().ToArray();
        Assert.Equal(store.OriginalConstraintCount, constraints.Length);
        var constraintsList = string.Join(Environment.NewLine,
            constraints.Select(constraint => string.Join(" ", constraint.Literals.Select(l => l.Orientation ? $"{l.Variable.Index+1}" : $"-{l.Variable.Index+1}")))
            .OrderBy(s => s));

        const string expectedConstraints = @"
-1 -3 6
-8
1 2
2 -7 -8
9";
        Assert.Equal(expectedConstraints.Trim(), constraintsList);

        Assert.Equal([3, 4, 9, 2, 5, 6, 0, 1, 8, 7], 
            store.Variables.OrderBy(v => v.Activity).Select(v => v.Index));
        Assert.True(store.Variables.Select(v => v.Activity).Max() < 1);
        Assert.True(store.Variables.Select(v => v.Activity).Min() >= 0);

        Assert.True(store.Variables[0].Polarity);
        Assert.True(store.Variables[1].Polarity);
        Assert.False(store.Variables[2].Polarity);
        Assert.False(store.Variables[3].Polarity);
        Assert.False(store.Variables[4].Polarity);
        Assert.True(store.Variables[5].Polarity);
        Assert.False(store.Variables[6].Polarity);
        Assert.False(store.Variables[7].Polarity);
        Assert.True(store.Variables[8].Polarity);
        Assert.False(store.Variables[9].Polarity);
    }

    [Fact]
    public void SingleLiteral()
    {
        var problem = new Problem(1, [new Clause([1])]);
        var options = new SatSolverOptions
        {
            Restart = new() { Luby = false, Interval = 10 } // for coverage...
        };

        var store = new SatSolverInitializer(problem, options, default).Initialize();

        Assert.Equal(1, store.OriginalConstraintCount);
        Assert.Single(store.Variables);
        Assert.True(store.Variables[0].Polarity);
        Assert.Single(store.UnitPropagationQueue);
    }

    [Fact]
    public void EmptyProblem()
    {
        var problem = new Problem(0, []);
        var options = new SatSolverOptions
        {
            Restart = new() { Luby = true, Interval = null } // for coverage...
        };

        var store = new SatSolverInitializer(problem, options, default).Initialize();
        Assert.Equal(0, store.OriginalConstraintCount);
        Assert.Empty(store.Variables);
    }

    [Fact]
    public void ThreeStateSudoku_CorrectWatchers()
    {
        var problem = DimacsParser.Parse(Problems.ThreeStateSudoku).Single();
        var options = new SatSolverOptions
        {
            Restart = new() { Luby = true, Interval = 10 } // for coverage...
        };
        var store = new SatSolverInitializer(problem, options, default).Initialize();

        Assert.Equal("-1 -2 | -1 -3",
            string.Join(" | ",
                store.Variables[0].NegativeLiteral.Watchers.Select(constraint => string.Join(" ", constraint.Literals.Select(literal => literal.Orientation ? literal.Variable.Index + 1 : -(literal.Variable.Index + 1))))));
    }
}