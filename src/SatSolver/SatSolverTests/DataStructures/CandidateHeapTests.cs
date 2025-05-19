using Revo.SatSolver.DataStructures;

namespace SatSolverTests.DataStructures;

public sealed class CandidateHeapTests
{
    [Fact]
    public void Initialized_CorrectSequence()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].Activity = 5;
        variables[1].Activity = 8;
        variables[2].Activity = 2;
        variables[3].Activity = 7;
        variables[4].Activity = 9;

        var sut = new CandidateHeap(variables);

        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Equal(1, sut.Dequeue()!.Index);
        Assert.Equal(3, sut.Dequeue()!.Index);
        Assert.Equal(0, sut.Dequeue()!.Index);
        Assert.Equal(2, sut.Dequeue()!.Index);
        Assert.Null(sut.Dequeue());
    }
    [Fact]
    public void Initialized_IgnoreAlreadyFixedVariables()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].Activity = 5;
        variables[1].Activity = 8;
        variables[2].Activity = 2;
        variables[3].Activity = 7;
        variables[4].Activity = 9;

        variables[2].Sense = true;
        variables[4].Sense = false;

        var sut = new CandidateHeap(variables);

        Assert.Equal(1, sut.Dequeue()!.Index);
        Assert.Equal(3, sut.Dequeue()!.Index);
        Assert.Equal(0, sut.Dequeue()!.Index);
        Assert.Null(sut.Dequeue());
    }

    [Fact]
    public void Dequeued_EnqueuedSmaller_CorrectSequence()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].Activity = 5;
        variables[1].Activity = 8;
        variables[2].Activity = 2;
        variables[3].Activity = 7;
        variables[4].Activity = 9;

        var sut = new CandidateHeap(variables);
        Assert.Equal(4, sut.Dequeue()!.Index);

        variables[4].Activity = 3;
        variables[4].DecisionLevel = 12;
        variables[4].Reason = new Constraint([variables[4].PositiveLiteral]);
        variables[4].Sense = true;
        sut.Enqueue([variables[4]]);
        Assert.Equal(0, variables[4].DecisionLevel);
        Assert.Null(variables[4].Reason);
        Assert.Null(variables[4].Sense);

        Assert.Equal(1, sut.Dequeue()!.Index);
        Assert.Equal(3, sut.Dequeue()!.Index);
        Assert.Equal(0, sut.Dequeue()!.Index);
        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Equal(2, sut.Dequeue()!.Index);
        Assert.Null(sut.Dequeue());
    }
    [Fact]
    public void Dequeued_EnqueuedGreater_CorrectSequence()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].Activity = 5;
        variables[1].Activity = 8;
        variables[2].Activity = 2;
        variables[3].Activity = 7;
        variables[4].Activity = 9;

        var sut = new CandidateHeap(variables);
        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Equal(1, sut.Dequeue()!.Index);
        variables[1].Activity = 10;
        variables[1].DecisionLevel = 12;
        variables[1].Reason = new Constraint([variables[1].PositiveLiteral]);
        variables[1].Sense = true;
        sut.Enqueue([variables[1]]);
        Assert.Equal(0, variables[1].DecisionLevel);
        Assert.Null(variables[1].Reason);
        Assert.Null(variables[1].Sense);

        Assert.Equal(1, sut.Dequeue()!.Index);
        Assert.Equal(3, sut.Dequeue()!.Index);
        Assert.Equal(0, sut.Dequeue()!.Index);
        Assert.Equal(2, sut.Dequeue()!.Index);
        Assert.Null(sut.Dequeue());
    }
    [Fact]
    public void Update_First_Smaller_CorrectSequence()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].Activity = 5;
        variables[1].Activity = 8;
        variables[2].Activity = 2;
        variables[3].Activity = 7;
        variables[4].Activity = 9;

        var sut = new CandidateHeap(variables);
        variables[4].Activity = 3;
        variables[4].DecisionLevel = 12;
        variables[4].Reason = new Constraint([variables[4].PositiveLiteral]);
        variables[4].Sense = true;
        sut.Enqueue([variables[4]]);
        Assert.Equal(0, variables[4].DecisionLevel);
        Assert.Null(variables[4].Reason);
        Assert.Null(variables[4].Sense);

        Assert.Equal(1, sut.Dequeue()!.Index);
        Assert.Equal(3, sut.Dequeue()!.Index);
        Assert.Equal(0, sut.Dequeue()!.Index);
        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Equal(2, sut.Dequeue()!.Index);
        Assert.Null(sut.Dequeue());
    }
    [Fact]
    public void Update_First_Greater_CorrectSequence()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].Activity = 5;
        variables[1].Activity = 8;
        variables[2].Activity = 2;
        variables[3].Activity = 7;
        variables[4].Activity = 9;

        var sut = new CandidateHeap(variables);
        variables[4].Activity = 12;
        variables[4].DecisionLevel = 12;
        variables[4].Reason = new Constraint([variables[4].PositiveLiteral]);
        variables[4].Sense = true;
        sut.Enqueue([variables[4]]);
        Assert.Equal(0, variables[4].DecisionLevel);
        Assert.Null(variables[4].Reason);
        Assert.Null(variables[4].Sense);

        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Equal(1, sut.Dequeue()!.Index);
        Assert.Equal(3, sut.Dequeue()!.Index);
        Assert.Equal(0, sut.Dequeue()!.Index);
        Assert.Equal(2, sut.Dequeue()!.Index);
        Assert.Null(sut.Dequeue());
    }
    [Fact]
    public void Update_Last_Smaller_CorrectSequence()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].Activity = 10;
        variables[1].Activity = 8;
        variables[2].Activity = 6;
        variables[3].Activity = 4;
        variables[4].Activity = 2;

        var sut = new CandidateHeap(variables);
        variables[4].Activity = 1;
        variables[4].DecisionLevel = 12;
        variables[4].Reason = new Constraint([variables[4].PositiveLiteral]);
        variables[4].Sense = true;
        sut.Enqueue([variables[4]]);
        Assert.Equal(0, variables[4].DecisionLevel);
        Assert.Null(variables[4].Reason);
        Assert.Null(variables[4].Sense);

        Assert.Equal(0, sut.Dequeue()!.Index);
        Assert.Equal(1, sut.Dequeue()!.Index);
        Assert.Equal(2, sut.Dequeue()!.Index);
        Assert.Equal(3, sut.Dequeue()!.Index);
        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Null(sut.Dequeue());
    }
    [Fact]
    public void Update_Last_Greater_CorrectSequence()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].Activity = 10;
        variables[1].Activity = 8;
        variables[2].Activity = 6;
        variables[3].Activity = 4;
        variables[4].Activity = 2;

        var sut = new CandidateHeap(variables);
        variables[4].Activity = 5;
        variables[4].DecisionLevel = 12;
        variables[4].Reason = new Constraint([variables[4].PositiveLiteral]);
        variables[4].Sense = true;
        sut.Enqueue([variables[4]]);
        Assert.Equal(0, variables[4].DecisionLevel);
        Assert.Null(variables[4].Reason);
        Assert.Null(variables[4].Sense);

        Assert.Equal(0, sut.Dequeue()!.Index);
        Assert.Equal(1, sut.Dequeue()!.Index);
        Assert.Equal(2, sut.Dequeue()!.Index);
        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Equal(3, sut.Dequeue()!.Index);
        Assert.Null(sut.Dequeue());
    }
    [Fact]
    public void Update_Inner_Smaller_CorrectSequence()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].Activity = 10;
        variables[1].Activity = 8;
        variables[2].Activity = 6;
        variables[3].Activity = 4;
        variables[4].Activity = 2;

        var sut = new CandidateHeap(variables);
        variables[2].Activity = 3;
        variables[2].DecisionLevel = 12;
        variables[2].Reason = new Constraint([variables[2].PositiveLiteral]);
        variables[2].Sense = true;
        sut.Enqueue([variables[2]]);
        Assert.Equal(0, variables[2].DecisionLevel);
        Assert.Null(variables[2].Reason);
        Assert.Null(variables[2].Sense);

        Assert.Equal(0, sut.Dequeue()!.Index);
        Assert.Equal(1, sut.Dequeue()!.Index);
        Assert.Equal(3, sut.Dequeue()!.Index);
        Assert.Equal(2, sut.Dequeue()!.Index);
        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Null(sut.Dequeue());
    }
    [Fact]
    public void Update_Inner_Greater_CorrectSequence()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].Activity = 10;
        variables[1].Activity = 8;
        variables[2].Activity = 6;
        variables[3].Activity = 4;
        variables[4].Activity = 2;

        var sut = new CandidateHeap(variables);
        variables[2].Activity = 9;
        variables[2].DecisionLevel = 12;
        variables[2].Reason = new Constraint([variables[2].PositiveLiteral]);
        variables[2].Sense = true;
        sut.Enqueue([variables[2]]);
        Assert.Equal(0, variables[2].DecisionLevel);
        Assert.Null(variables[2].Reason);
        Assert.Null(variables[2].Sense);

        Assert.Equal(0, sut.Dequeue()!.Index);
        Assert.Equal(2, sut.Dequeue()!.Index);
        Assert.Equal(1, sut.Dequeue()!.Index);
        Assert.Equal(3, sut.Dequeue()!.Index);
        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Null(sut.Dequeue());
    }
}
