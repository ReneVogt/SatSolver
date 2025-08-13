using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;

namespace SatSolverTests.DPLL;

public sealed class DpllProcessorTests
{
    sealed class TestTrail : IVariableTrail
    {
        public List<Variable> AddedVariables { get; } = [];
        public Variable this[int index] => throw new NotImplementedException();
        public int Count => throw new NotImplementedException();
        public int DecisionLevel => 0;
        public void Add(Variable variable) => AddedVariables.Add(variable);
        public (Variable? candidate, bool sense) Backtrack() => throw new NotImplementedException();
        public void Clear() => throw new NotImplementedException();
        public void JumpBack(int level) => throw new NotImplementedException();
        public void Push(bool firstTryOfCandidate) => throw new NotImplementedException();
        public void Reset() => throw new NotImplementedException();
    }

    sealed class TestActivityManager : IActivityManager
    {
        public List<Constraint> IncreasedConstraints { get; } = [];
        public double ConstraintActivityIncrement => throw new NotImplementedException();
        public double VariableActivityIncrement => throw new NotImplementedException();

        public void DecayConstraintActivity() => throw new NotImplementedException();
        public void IncreaseConstraintActivity(Constraint constraint, double factor = 1)
        {
            Assert.Equal(0.5, factor);
            IncreasedConstraints.Add(constraint);
        }
        public void IncreaseVariableActivity(Constraint constraint) => throw new NotImplementedException();
    }

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

        var trail = new TestTrail();
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

        var trail = new TestTrail();
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

        Assert.Equal([constraint5, constraint6], activityManager.IncreasedConstraints);
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

        var trail = new TestTrail();
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

        var trail = new TestTrail();
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

        var trail = new TestTrail();
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

        Assert.Equal([constraint1], activityManager.IncreasedConstraints);
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

        var trail = new TestTrail();
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

        Assert.Equal([constraint1], activityManager.IncreasedConstraints);
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

        var trail = new TestTrail();
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
}
