using Moq;
using Revo.SatSolver;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;

namespace SatSolverTests.Tools;

public sealed class ActivityManagerTests
{
    [Fact]
    public void IncreaseVariableActivity_IncreasesVariableActivity()
    {
        var candidateHeap = new Mock<ICandidateHeap>();
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i) { Activity = i}).ToArray();

        var options = new SatSolverOptions
        {
            VariableActivityDecayFactor = 0.5d,
            ConstraintActivityDecayFactor = 0.7d
        };

        var sut = new ActivityManager<ICandidateHeap>(variables, [], candidateHeap.Object, options);

        Assert.Equal(1, sut.VariableActivityIncrement);
        Assert.All(variables, v => Assert.Equal(v.Index, v.Activity));

        var constraint = new Constraint([variables[5].PositiveLiteral, variables[3].NegativeLiteral]);
        sut.IncreaseVariableActivity(constraint);

        candidateHeap.Verify(heap => heap.Rescale(It.IsAny<double>()), Times.Never);
        Assert.Equal(2, sut.VariableActivityIncrement);
        Assert.Equal(6, variables[5].Activity);
        Assert.Equal(4, variables[3].Activity);
        Assert.All(variables.Where(v => v.Index != 5 && v.Index != 3), v => Assert.Equal(v.Index, v.Activity));

        sut.IncreaseVariableActivity(constraint);

        candidateHeap.Verify(heap => heap.Rescale(It.IsAny<double>()), Times.Never);
        Assert.Equal(4, sut.VariableActivityIncrement);
        Assert.Equal(8, variables[5].Activity);
        Assert.Equal(6, variables[3].Activity);
        Assert.All(variables.Where(v => v.Index != 5 && v.Index != 3), v => Assert.Equal(v.Index, v.Activity));
    }
    [Fact]
    public void IncreaseVariableActivity_RescaleWhenNeeded()
    {
        var candidateHeap = new Mock<ICandidateHeap>();
        var variables = new[] { new Variable(0) { Activity = 1e100 - 1 } };

        var options = new SatSolverOptions
        {
            VariableActivityDecayFactor = 0.5d,
            ConstraintActivityDecayFactor = 0.7d
        };
        var sut = new ActivityManager<ICandidateHeap>(variables, [], candidateHeap.Object, options);
        var constraint = new Constraint([variables[0].PositiveLiteral]);
        sut.IncreaseVariableActivity(constraint);
        candidateHeap.Verify(heap => heap.Rescale(It.IsAny<double>()), Times.Once);
        Assert.Equal(2e-100, sut.VariableActivityIncrement);
        Assert.Equal(1, variables[0].Activity);
    }
    [Fact]
    public void IncreaseConstraintActivity_OnlyIfTracked()
    {
        var candidateHeap = new Mock<ICandidateHeap>();
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i) { Activity = i }).ToArray();
        var constraints = new List<Constraint>();

        var options = new SatSolverOptions
        {
            VariableActivityDecayFactor = 0.7d,
            ConstraintActivityDecayFactor = 0.5d
        };
        constraints.Add(new([new Variable(0).PositiveLiteral]) { Activity = 12, IsTracked = true });
        var sut = new ActivityManager<ICandidateHeap>(variables, constraints, candidateHeap.Object, options);
        var constraint = new Constraint([new Variable(1).PositiveLiteral]) { Activity = 23, IsTracked = false };

        Assert.Equal(1, sut.ConstraintActivityIncrement);

        sut.IncreaseConstraintActivity(constraint);
        candidateHeap.Verify(heap => heap.Rescale(It.IsAny<double>()), Times.Never);
        Assert.Equal(1, sut.ConstraintActivityIncrement);
        Assert.Equal(12, constraints[0].Activity);
        Assert.Equal(23, constraint.Activity);

        constraint.IsTracked = true;
        sut.IncreaseConstraintActivity(constraint);

        candidateHeap.Verify(heap => heap.Rescale(It.IsAny<double>()), Times.Never);
        Assert.Equal(1, sut.ConstraintActivityIncrement);
        Assert.Equal(12, constraints[0].Activity);
        Assert.Equal(24, constraint.Activity);
    }
    [Fact]
    public void DecayConstraintActivity_HigherIncrement()
    {
        var candidateHeap = new Mock<ICandidateHeap>();
        var constraint = new Constraint([new Variable(1).PositiveLiteral]) { Activity = 23, IsTracked = true };
        var constraints = new List<Constraint>();
        var options = new SatSolverOptions
        {
            VariableActivityDecayFactor = 0.7d,
            ConstraintActivityDecayFactor = 0.5d
        };
        constraints.Add(new([new Variable(0).PositiveLiteral]) { Activity = 12, IsTracked = true });

        var sut = new ActivityManager<ICandidateHeap>([], constraints, candidateHeap.Object, options);

        Assert.Equal(1, sut.ConstraintActivityIncrement);

        sut.IncreaseConstraintActivity(constraint);
        candidateHeap.Verify(heap => heap.Rescale(It.IsAny<double>()), Times.Never);
        Assert.Equal(1, sut.ConstraintActivityIncrement);
        Assert.Equal(12, constraints[0].Activity);
        Assert.Equal(24, constraint.Activity);

        sut.DecayConstraintActivity();
        Assert.Equal(2, sut.ConstraintActivityIncrement);
        sut.IncreaseConstraintActivity(constraint);

        candidateHeap.Verify(heap => heap.Rescale(It.IsAny<double>()), Times.Never);
        Assert.Equal(2, sut.ConstraintActivityIncrement);
        Assert.Equal(12, constraints[0].Activity);
        Assert.Equal(26, constraint.Activity);
    }
    [Fact]
    public void IncreaseConstraintActivity_RescaleWhenNeeded()
    {
        var candidateHeap = new Mock<ICandidateHeap>();
        var options = new SatSolverOptions
        {
            VariableActivityDecayFactor = 0.7d,
            ConstraintActivityDecayFactor = 0.5d
        };
        var constraints = new List<Constraint>();
        constraints.AddRange([
            new([new Variable(0).PositiveLiteral]) { Activity = 1, IsTracked = true },
            new ([new Variable(1).PositiveLiteral]) { Activity = 1e100-1, IsTracked = true }]);
        var constraint = constraints[1];

        var sut = new ActivityManager<ICandidateHeap>([], constraints, candidateHeap.Object, options);

        Assert.Equal(1, sut.ConstraintActivityIncrement);

        sut.IncreaseConstraintActivity(constraint);
        candidateHeap.Verify(heap => heap.Rescale(It.IsAny<double>()), Times.Never);
        Assert.Equal(1e-100, sut.ConstraintActivityIncrement);
        Assert.Equal(1e-100, constraints[0].Activity);
        Assert.Equal(1, constraint.Activity);
    }
}
