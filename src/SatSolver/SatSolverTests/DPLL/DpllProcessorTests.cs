using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
using SatSolverTests.Stubs;

namespace SatSolverTests.DPLL;

public sealed class DpllProcessorTests
{
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
        var constraint0 = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint1 = new Constraint([variables[0].NegativeLiteral, variables[1].NegativeLiteral, variables[2].NegativeLiteral]);
        var constraint2 = new Constraint([variables[0].NegativeLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint3 = new Constraint([variables[0].PositiveLiteral, variables[1].NegativeLiteral, variables[2].PositiveLiteral]);
        var constraint4 = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].NegativeLiteral]);
        var constraint5 = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral]);
        var constraint6 = new Constraint([variables[0].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint7 = new Constraint([variables[1].PositiveLiteral, variables[2].PositiveLiteral]);

        var trail = new TestVariableTrail();
        var activityManager = new TestActivityManager();
        var units = new Queue<(ConstraintLiteral Literal, Constraint Reason)>();

        var sut = new DpllProcessor(trail, units, activityManager, default);

        var conflict = sut.PropagateVariable(variables[0], true, null, out var propagations);
        Assert.Null(conflict);

        Assert.Equal([variables[0]], trail.AddedVariables);
        Assert.True(variables[0].Sense);
        Assert.True(variables[0].Polarity);

        Assert.Equal(variables[1].NegativeLiteral, constraint1.Watched1);
        Assert.Equal(variables[2].NegativeLiteral, constraint1.Watched2);

        Assert.Equal(variables[1].PositiveLiteral, constraint2.Watched1);
        Assert.Equal(variables[2].PositiveLiteral, constraint2.Watched2);

        Assert.Empty(activityManager.IncreasedConstraints);
        Assert.Empty(units);
        Assert.Equal(0, propagations);
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
        var constraint0 = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint1 = new Constraint([variables[0].NegativeLiteral, variables[1].NegativeLiteral, variables[2].NegativeLiteral]);
        var constraint2 = new Constraint([variables[0].NegativeLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint3 = new Constraint([variables[0].PositiveLiteral, variables[1].NegativeLiteral, variables[2].PositiveLiteral]);
        var constraint4 = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].NegativeLiteral]);
        var constraint5 = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral]);
        var constraint6 = new Constraint([variables[0].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint7 = new Constraint([variables[1].PositiveLiteral, variables[2].PositiveLiteral]);

        var trail = new TestVariableTrail();
        var activityManager = new TestActivityManager();
        var units = new Queue<(ConstraintLiteral Literal, Constraint Reason)>();

        var sut = new DpllProcessor(trail, units, activityManager, default);

        var conflict = sut.PropagateVariable(variables[0], false, null, out var propagations);
        Assert.Null(conflict);

        Assert.Equal([variables[0]], trail.AddedVariables);
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

        Assert.Equal([(constraint5, 0.5d), (constraint6, 0.5d)], activityManager.IncreasedConstraints);
        Assert.Equal([(variables[1].PositiveLiteral, constraint5), (variables[2].PositiveLiteral, constraint6)], units);
        Assert.Equal(2, propagations);
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
        var constraint0 = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint1 = new Constraint([variables[0].NegativeLiteral, variables[1].NegativeLiteral, variables[2].NegativeLiteral]);

        var trail = new TestVariableTrail();
        var activityManager = new TestActivityManager();
        var units = new Queue<(ConstraintLiteral Literal, Constraint Reason)>();

        var sut = new DpllProcessor(trail, units, activityManager, default);

        var conflict = sut.PropagateVariable(variables[0], true, null, out var propagations);
        Assert.Equal(constraint1, conflict);

        Assert.Equal([variables[0]], trail.AddedVariables);
        Assert.True(variables[0].Sense);
        Assert.False(variables[0].Polarity);

        Assert.Equal(variables[0].PositiveLiteral, constraint0.Watched1);
        Assert.Equal(variables[1].PositiveLiteral, constraint0.Watched2);

        Assert.Equal(variables[1].NegativeLiteral, constraint1.Watched1);
        Assert.Equal(variables[0].NegativeLiteral, constraint1.Watched2);

        Assert.Empty(activityManager.IncreasedConstraints);
        Assert.Empty(units);
        Assert.Equal(0, propagations);
    }

    [Fact]
    public void PropagateVariable_AlreadyTrueConstraints()
    {
        // p cnf 3 2
        // 1 2 3 0
        // 1 3 2 0

        var variables = Enumerable.Range(0, 3).Select(i => new Variable(i)).ToArray();
        variables[2].Sense = true;
        var constraint0 = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint1 = new Constraint([variables[0].PositiveLiteral, variables[2].PositiveLiteral, variables[1].PositiveLiteral]);

        var trail = new TestVariableTrail();
        var activityManager = new TestActivityManager();
        var units = new Queue<(ConstraintLiteral Literal, Constraint Reason)>();

        var sut = new DpllProcessor(trail, units, activityManager, default);

        var conflict = sut.PropagateVariable(variables[0], false, null, out var propagations);
        Assert.Null(conflict);

        Assert.Equal([variables[0]], trail.AddedVariables);
        Assert.False(variables[0].Sense);
        Assert.False(variables[0].Polarity);

        Assert.Equal(variables[1].PositiveLiteral, constraint0.Watched1);
        Assert.Equal(variables[2].PositiveLiteral, constraint0.Watched2);

        Assert.Equal(variables[2].PositiveLiteral, constraint1.Watched1);
        Assert.Equal(variables[0].PositiveLiteral, constraint1.Watched2);

        Assert.Empty(activityManager.IncreasedConstraints);
        Assert.Empty(units);
        Assert.Equal(0, propagations);
    }
    [Fact]
    public void PropagateUnits_WithConflict() 
    {
        // p cnf 3 2
        // 1 2 3 0
        // -1 -2 -3 0

        var variables = Enumerable.Range(0, 3).Select(i => new Variable(i)).ToArray();
        var constraint0 = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint1 = new Constraint([variables[0].NegativeLiteral, variables[1].NegativeLiteral, variables[2].NegativeLiteral]);

        var trail = new TestVariableTrail();
        var activityManager = new TestActivityManager();
        var units = new Queue<(ConstraintLiteral Literal, Constraint Reason)>();
        units.Enqueue((variables[0].PositiveLiteral, constraint0));
        units.Enqueue((variables[1].PositiveLiteral, constraint0));
        units.Enqueue((variables[2].PositiveLiteral, constraint0));

        var sut = new DpllProcessor(trail, units, activityManager, default);

        var propagations = 0;
        var conflict = sut.PropagateUnits(ref propagations);
        Assert.Equal(constraint1, conflict);

        Assert.Equal([variables[0], variables[1], variables[2]], trail.AddedVariables);
        Assert.True(variables[1].Sense);
        Assert.True(variables[1].Polarity);
        Assert.True(variables[2].Sense);
        Assert.False(variables[2].Polarity);

        Assert.Equal([(constraint1, 0.5d)], activityManager.IncreasedConstraints);
        Assert.Equal([(variables[2].NegativeLiteral, constraint1)], units);
        Assert.Equal(1, propagations);
    }
    [Fact]
    public void PropagateUnits_WithoutConflict()
    {
        // p cnf 3 2
        // 1 2 3 0
        // -1 -2 -3 0

        var variables = Enumerable.Range(0, 3).Select(i => new Variable(i)).ToArray();
        var constraint0 = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral, variables[2].PositiveLiteral]);
        var constraint1 = new Constraint([variables[0].NegativeLiteral, variables[1].NegativeLiteral, variables[2].NegativeLiteral]);

        var trail = new TestVariableTrail();
        var activityManager = new TestActivityManager();
        var units = new Queue<(ConstraintLiteral Literal, Constraint Reason)>();
        units.Enqueue((variables[0].PositiveLiteral, constraint0));
        units.Enqueue((variables[1].PositiveLiteral, constraint0));

        var sut = new DpllProcessor(trail, units, activityManager, default);

        var propagations = 0;
        var conflict = sut.PropagateUnits(ref propagations);
        Assert.Null(conflict);

        Assert.Equal([variables[0], variables[1], variables[2]], trail.AddedVariables);
        Assert.True(variables[1].Sense);
        Assert.True(variables[1].Polarity);

        Assert.Equal([(constraint1, 0.5d)], activityManager.IncreasedConstraints);
        //Assert.Equal([(variables[2].NegativeLiteral, constraint1)], units);
        Assert.Empty(units);
        Assert.Equal(1, propagations);
    }
    [Fact]
    public void PropagateUnits_AlreadyPropagatedSkipped()
    {
        // This test would lead to a conflict if
        // the unit was really propagated. So
        // we can verify that an already assigned
        // variable is not propagated again.
       
        // p cnf 2 1
        // 1 2 0

        var variables = Enumerable.Range(0, 2).Select(i => new Variable(i)).ToArray();
        var constraint = new Constraint([variables[0].PositiveLiteral, variables[1].PositiveLiteral]);

        var trail = new TestVariableTrail();
        var activityManager = new TestActivityManager();
        var units = new Queue<(ConstraintLiteral Literal, Constraint Reason)>();
        units.Enqueue((variables[0].NegativeLiteral, constraint));
        variables[0].Sense = false; // this should avoid propagation of -v0
        variables[1].Sense = false; // this should lead to a conflict if -v0 is propagated

        var sut = new DpllProcessor(trail, units, activityManager, default);

        var propagations = 0;
        var conflict = sut.PropagateUnits(ref propagations);
        Assert.Null(conflict);

        Assert.Empty(trail.AddedVariables);
        Assert.False(variables[0].Sense);
        Assert.False(variables[1].Sense);

        Assert.Empty(activityManager.IncreasedConstraints);
        Assert.Empty(units);
        Assert.Equal(0, propagations);
    }


    [Fact]
    public void PropagateUnits_ConflictingPropagations()
    {
        // p cnf 2 2
        // 1 2 0
        // 1 -2 0

        var variables = Enumerable.Range(0, 2).Select(i => new Variable(i)).ToArray();
        var v0 = variables[0]; var v0p = v0.PositiveLiteral; var v0n = v0.NegativeLiteral;
        var v1 = variables[1]; var v1p = v1.PositiveLiteral; var v1n = v1.NegativeLiteral;
        var constraint0 = new Constraint([v0p, v1p], setWatchers: false)
        {
            Watched1 = v0p,
            Watched2 = v1p
        };
        v0p.Watchers.Add(constraint0);
        v1p.Watchers.Add(constraint0);
        var constraint1 = new Constraint([v0p, v1n], setWatchers: false)
        {
            Watched1 = v0p,
            Watched2 = v1n
        };
        v0p.Watchers.Add(constraint1);
        v1n.Watchers.Add(constraint1);

        var trail = new TestVariableTrail();
        var activityManager = new TestActivityManager();
        var units = new Queue<(ConstraintLiteral Literal, Constraint Reason)>();

        var sut = new DpllProcessor(trail, units, activityManager, default);

        var conflict = sut.PropagateVariable(variables[0], false, null, out var propagations);
        Assert.Null(conflict);
        Assert.Equal([(v1p, constraint0), (v1n, constraint1)], units);

        var props = 0;
        conflict = sut.PropagateUnits(ref props);
        Assert.Equal(constraint1, conflict);
        Assert.Equal(0, props);
    }
}
