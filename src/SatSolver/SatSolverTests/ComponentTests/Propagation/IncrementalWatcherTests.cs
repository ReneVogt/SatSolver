using Revo.SatSolver;
using TestHelpers;
using static Revo.SatSolver.SatSolverFactory;

namespace SatSolverTests.ComponentTests.Propagation;

[Trait("TestLevel", "Component")]
public sealed class IncrementalWatcherTests
{
    [Theory]
    [InlineData(0, false, false)]
    [InlineData(1, false, false)]
    [InlineData(0, true, false)]
    [InlineData(0, false, null)]
    [InlineData(1, false, null)]
    [InlineData(0, true, null)]
    [InlineData(0, false, true)]
    [InlineData(1, false, true)]
    [InlineData(0, true, true)]
    public void AddClause_Satisfied_PropagatesAfterBackjumpOrRestart(int targetLevel, bool restart, bool? otherSense)
    {
        var scenario = new Scenario();
        if (targetLevel == 1) scenario.Decide(-4);
        scenario.Decide(-3);
        if (otherSense is not null) scenario.Decide(otherSense.Value ? 2 : -2);

        var clause = new Clause([1, 2, -3]);
        scenario.Solver.AddClause(clause);
        var constraint = Assert.Single(scenario.Store.Variables[2].NegativeLiteral.Watchers);

        if (restart)
            scenario.Restart();
        else
            scenario.Store.VariableTrail.JumpBack(targetLevel);
        Assert.Equal(targetLevel, scenario.Store.VariableTrail.DecisionLevel);

        scenario.Decide(3);
        scenario.PropagateUnits();

        var forcedVariable = scenario.Store.Variables[1];
        Assert.True(forcedVariable.Sense);
        Assert.Equal(targetLevel+1, forcedVariable.DecisionLevel);
        Assert.Same(constraint, forcedVariable.Reason);
        var solution = scenario.Solver.FindSolution();
        Assert.NotNull(solution);
        SolutionValidator.Validate(new Problem(4, [new([-1]), clause]), solution);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public void AddClause_OnlyNonFalseLiteral_IsPropagatedAtRequiredLevel(int clauseLength, bool alreadyTrue)
    {
        var scenario = new Scenario();
        scenario.Decide(-2);
        scenario.Decide(-4);
        if (alreadyTrue) scenario.Decide(3);
        var clause = clauseLength switch
        {
            1 => new Clause([3]),
            2 => new Clause([1, 3]),
            _ => new Clause([1, 2, 3])
        };
        var expectedLevel = clauseLength == 3 ? 1 : 0;

        scenario.Solver.AddClause(clause);

        Assert.Equal(expectedLevel, scenario.Store.VariableTrail.DecisionLevel);
        var (literal, reason) = Assert.Single(scenario.Store.UnitPropagationQueue);
        Assert.Same(scenario.Store.Variables[2].PositiveLiteral, literal);
        Assert.NotNull(reason);
        Assert.True(reason.IsAdditional);
        scenario.PropagateUnits();
        Assert.True(literal.Sense);
        Assert.Equal(expectedLevel, literal.Variable.DecisionLevel);
        Assert.Same(reason, literal.Variable.Reason);

        scenario.Restart();
        if (expectedLevel == 0)
            Assert.True(literal.Sense);
        else
        {
            Assert.Null(literal.Sense);
            scenario.Decide(-2);
            scenario.PropagateUnits();
            Assert.True(literal.Sense);
            Assert.Same(reason, literal.Variable.Reason);
        }
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(2)]
    public void AddClause_WithPendingUnitAndBackjump_PreservesValidImplications(int nextUnit)
    {
        var scenario = new Scenario();
        scenario.Decide(-2);
        scenario.Decide(-4);
        var clause = new Clause([1, 2, 3]);
        scenario.Solver.AddClause(clause);
        Assert.NotEmpty(scenario.Store.UnitPropagationQueue);

        // -2 lowers a satisfied unit to the root. +2 conflicts with the current
        // assignment and invalidates the pending implication of +3 by this clause.
        scenario.Solver.AddClause(new Clause([nextUnit]));
        var solution = scenario.Solver.FindSolution();

        Assert.NotNull(solution);
        SolutionValidator.Validate(new Problem(4, [new([-1]), clause, new([nextUnit])]), solution);
        Assert.Equal(0, scenario.Store.Variables[1].DecisionLevel);
        if (nextUnit < 0)
        {
            Assert.True(scenario.Store.Variables[2].Sense);
            Assert.Equal(0, scenario.Store.Variables[2].DecisionLevel);
        }
        else
            Assert.Null(scenario.Store.Variables[2].Reason);
    }

    // Drive real components to make decision levels independent of search heuristics.
    sealed class Scenario
    {
        public ComponentStore Store { get; }
        public ISatSolver Solver { get; }

        public Scenario()
        {
            var options = new SatSolverOptions
            {
                Restart = new()
                {
                    Interval = 1,
                    ByLiteralBlockDistance = false,
                    ByPropagationRate = false
                },
                ConstraintDeletion = new() { ReduceOnRestart = false }
            };
            Store = new ComponentStore(options, new Problem(4, [new([-1])]));
            Solver = Create(Store);
            PropagateUnits();
        }

        public void Decide(int literal)
        {
            var variable = Store.Variables[Math.Abs(literal)-1];
            Assert.Null(variable.Sense);
            Store.VariableTrail.Push();
            Assert.Null(Store.VariablePropagator.PropagateVariable(variable, literal > 0, null));
        }

        public void PropagateUnits()
        {
            while (Store.UnitPropagationQueue.TryDequeue(out var entry))
            {
                var (literal, reason) = entry;
                Assert.NotNull(reason);
                if (literal.Sense is not null)
                    Assert.True(literal.Sense);
                else
                    Assert.Null(Store.VariablePropagator.PropagateVariable(literal.Variable, literal.Orientation, reason));
            }
        }

        public void Restart()
        {
            Store.RestartManager.AddConflict();
            Store.RestartManager.AddConflict();
            Assert.True(Store.RestartManager.RestartIfNecessary());
            Assert.Equal(0, Store.VariableTrail.DecisionLevel);
        }
    }
}
