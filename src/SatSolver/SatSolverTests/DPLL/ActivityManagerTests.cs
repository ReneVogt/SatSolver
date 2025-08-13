using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;

namespace SatSolverTests.DPLL;
public sealed class ActivityManagerTests
{
    sealed class CandidateHeapMock : ICandidateHeap
    {
        public int RescaleCalls { get; set; }
        public Variable? Dequeue() => throw new NotImplementedException();
        public void Enqueue(Span<Variable> variables) => throw new NotImplementedException();
        public void Rescale(double scaleLimit) => RescaleCalls++;
    }

    [Fact]
    public void IncreaseVariableActivity_IncreasesVariableActivity()
    {
        var candidateHeap = new CandidateHeapMock();
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i) { Activity = i}).ToArray();

        var sut = new ActivityManager(variables, [], 0.5, 0.7, candidateHeap);

        Assert.Equal(1, sut.VariableActivityIncrement);
        Assert.All(variables, v => Assert.Equal(v.Index, v.Activity));

        var constraint = new Constraint([variables[5].PositiveLiteral, variables[3].NegativeLiteral]);
        sut.IncreaseVariableActivity(constraint);

        Assert.Equal(0, candidateHeap.RescaleCalls);
        Assert.Equal(2, sut.VariableActivityIncrement);
        Assert.Equal(6, variables[5].Activity);
        Assert.Equal(4, variables[3].Activity);
        Assert.All(variables.Where(v => v.Index != 5 && v.Index != 3), v => Assert.Equal(v.Index, v.Activity));

        sut.IncreaseVariableActivity(constraint);

        Assert.Equal(0, candidateHeap.RescaleCalls);
        Assert.Equal(4, sut.VariableActivityIncrement);
        Assert.Equal(8, variables[5].Activity);
        Assert.Equal(6, variables[3].Activity);
        Assert.All(variables.Where(v => v.Index != 5 && v.Index != 3), v => Assert.Equal(v.Index, v.Activity));
    }
    [Fact]
    public void IncreaseVariableActivity_RescaleWhenNeeded()
    {
        var candidateHeap = new CandidateHeapMock();
        var variables = new[] { new Variable(0) { Activity = 1e100 - 1 } };

        var sut = new ActivityManager(variables, [], 0.5, 0.7, candidateHeap);
        var constraint = new Constraint([variables[0].PositiveLiteral]);
        sut.IncreaseVariableActivity(constraint);
        Assert.Equal(1, candidateHeap.RescaleCalls);
        Assert.Equal(2e-100, sut.VariableActivityIncrement);
        Assert.Equal(1, variables[0].Activity);
    }
    [Fact]
    public void IncreaseConstraintActivity_OnlyIfTracked()
    {
        var candidateHeap = new CandidateHeapMock();
        var constraints = new List<Constraint> { new([new Variable(0).PositiveLiteral]) { Activity = 12, IsTracked = true} };
        var constraint = new Constraint([new Variable(1).PositiveLiteral]) { Activity = 23, IsTracked = false };
        var sut = new ActivityManager([], constraints, 0.7, 0.5, candidateHeap);

        Assert.Equal(1, sut.ConstraintActivityIncrement);

        sut.IncreaseConstraintActivity(constraint);
        Assert.Equal(0, candidateHeap.RescaleCalls);
        Assert.Equal(1, sut.ConstraintActivityIncrement);
        Assert.Equal(12, constraints[0].Activity);
        Assert.Equal(23, constraint.Activity);

        constraint.IsTracked = true;
        sut.IncreaseConstraintActivity(constraint);

        Assert.Equal(0, candidateHeap.RescaleCalls);
        Assert.Equal(1, sut.ConstraintActivityIncrement);
        Assert.Equal(12, constraints[0].Activity);
        Assert.Equal(24, constraint.Activity);
    }
    [Fact]
    public void DecayConstraintActivity_HigherIncrement()
    {
        var candidateHeap = new CandidateHeapMock();
        var constraints = new List<Constraint> { new([new Variable(0).PositiveLiteral]) { Activity = 12, IsTracked = true } };
        var constraint = new Constraint([new Variable(1).PositiveLiteral]) { Activity = 23, IsTracked = true };
        var sut = new ActivityManager([], constraints, 0.7, 0.5, candidateHeap);

        Assert.Equal(1, sut.ConstraintActivityIncrement);

        sut.IncreaseConstraintActivity(constraint);
        Assert.Equal(0, candidateHeap.RescaleCalls);
        Assert.Equal(1, sut.ConstraintActivityIncrement);
        Assert.Equal(12, constraints[0].Activity);
        Assert.Equal(24, constraint.Activity);

        sut.DecayConstraintActivity();
        Assert.Equal(2, sut.ConstraintActivityIncrement);
        sut.IncreaseConstraintActivity(constraint);

        Assert.Equal(0, candidateHeap.RescaleCalls);
        Assert.Equal(2, sut.ConstraintActivityIncrement);
        Assert.Equal(12, constraints[0].Activity);
        Assert.Equal(26, constraint.Activity);
    }
    [Fact]
    public void IncreaseConstraintActivity_RescaleWhenNeeded()
    {
        var candidateHeap = new CandidateHeapMock();
        var constraints = new List<Constraint> { 
            new([new Variable(0).PositiveLiteral]) { Activity = 1, IsTracked = true },
            new ([new Variable(1).PositiveLiteral]) { Activity = 1e100-1, IsTracked = true }
        };
        var constraint = constraints[1];
        var sut = new ActivityManager([], constraints, 0.7, 0.5, candidateHeap);

        Assert.Equal(1, sut.ConstraintActivityIncrement);

        sut.IncreaseConstraintActivity(constraint);
        Assert.Equal(0, candidateHeap.RescaleCalls);
        Assert.Equal(1e-100, sut.ConstraintActivityIncrement);
        Assert.Equal(1e-100, constraints[0].Activity);
        Assert.Equal(1, constraint.Activity);
    }
}
