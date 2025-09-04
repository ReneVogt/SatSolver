using Revo.SatSolver;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Parsing;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace SatSolverTests.Processors;

public sealed class PreProcessorTests
{
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

        var store = new TestComponentStore(options, 10, _ => null!);
        var variables = store.Variables;
        var unitsToPropagate = new UnitPropagationQueue();
        var sut = new PreProcessor<ConstraintFactory>(options, problem, unitsToPropagate, variables, store.Literals, new ConstraintFactory([], []));
        var originalConstraintCount = sut.BuildConstraints();

        // two tautologies
        Assert.Equal(problem.Clauses.Length-2, originalConstraintCount);

        // unit propagations
        Assert.Equal([variables[7].NegativeLiteral, variables[8].PositiveLiteral], unitsToPropagate.Select(u => u.UnitLiteral).OrderBy(l => l.Variable.Index));

        // generated constraints
        var constraints = variables.SelectMany(v => v.PositiveLiteral.Watchers.Concat(v.NegativeLiteral.Watchers))
            .Distinct().ToArray();
        Assert.Equal(originalConstraintCount, constraints.Length);
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
            variables.OrderBy(v => v.Activity).Select(v => v.Index));
        Assert.True(variables.Select(v => v.Activity).Max() < 1);
        Assert.True(variables.Select(v => v.Activity).Min() >= 0);

        Assert.True(variables[0].Polarity);
        Assert.True(variables[1].Polarity);
        Assert.False(variables[2].Polarity);
        Assert.False(variables[3].Polarity);
        Assert.False(variables[4].Polarity);
        Assert.True(variables[5].Polarity);
        Assert.False(variables[6].Polarity);
        Assert.False(variables[7].Polarity);
        Assert.True(variables[8].Polarity);
        Assert.False(variables[9].Polarity);
    }

    [Fact]
    public void SingleLiteral()
    {
        var problem = new Problem(1, [new Clause([1])]);
        var options = new SatSolverOptions
        {
            Restart = new() { Luby = false, Interval = 10 } // for coverage...
        };

        var store = new TestComponentStore(options, 1, _ => null!);
        var variables = store.Variables;
        var unitsToPropagate = new UnitPropagationQueue();
        var sut = new PreProcessor<ConstraintFactory>(options, problem, unitsToPropagate, variables, store.Literals, new ConstraintFactory([], []));
        var originalConstraintCount = sut.BuildConstraints();        

        Assert.Equal(1, originalConstraintCount);
        Assert.Single(variables);
        Assert.True(variables[0].Polarity);
        Assert.Single(unitsToPropagate);
    }

    [Fact]
    public void EmptyProblem()
    {
        var problem = new Problem(0, []);
        var options = new SatSolverOptions
        {
            Restart = new() { Luby = true, Interval = null } // for coverage...
        };

        var store = new TestComponentStore(options, 0, _ => null!);
        var variables = store.Variables;
        var unitsToPropagate = new UnitPropagationQueue();
        var sut = new PreProcessor<ConstraintFactory>(options, problem, unitsToPropagate, variables, store.Literals, new ConstraintFactory([], []));
        var originalConstraintCount = sut.BuildConstraints();
        Assert.Equal(0, originalConstraintCount);
    }

    [Fact]
    public void ThreeStateSudoku_CorrectWatchers()
    {
        var problem = DimacsParser.Parse(Problems.ThreeStateSudoku).Single();
        var options = new SatSolverOptions
        {
            Restart = new() { Luby = true, Interval = 10 } // for coverage...
        };
        var store = new TestComponentStore(options, problem.NumberOfLiterals, _ => null!);
        var variables = store.Variables;
        var unitsToPropagate = new UnitPropagationQueue();
        var sut = new PreProcessor<ConstraintFactory>(options, problem, unitsToPropagate, variables, store.Literals, new ConstraintFactory([], []));
        var originalConstraintCount = sut.BuildConstraints();

        Assert.Equal("-1 -2 | -1 -3",
            string.Join(" | ",
                variables[0].NegativeLiteral.Watchers.Select(constraint => string.Join(" ", constraint.Literals.Select(literal => literal.Orientation ? literal.Variable.Index + 1 : -(literal.Variable.Index + 1))))));
    }
}