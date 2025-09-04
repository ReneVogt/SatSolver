using Moq;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;

namespace SatSolverTests.DataStructures;

public sealed class CandidateHeapTests
{
    static readonly ConstraintFactory _constraintFactory = new([], []);

    [Fact]
    public void Initialized_CorrectSequence()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].Activity = 5;
        variables[1].Activity = 8;
        variables[2].Activity = 2;
        variables[3].Activity = 7;
        variables[4].Activity = 9;

        var sut = new CandidateHeap<ConstraintFactory>(variables, null!);

        Assert.Equal(5, sut.Count);
        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Equal(1, sut.Dequeue()!.Index);
        Assert.Equal(3, sut.Dequeue()!.Index);
        Assert.Equal(0, sut.Dequeue()!.Index);
        Assert.Equal(2, sut.Dequeue()!.Index);
        Assert.Null(sut.Dequeue());
    }
    [Fact]
    public void Heapify_CorrectSequence()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();

        var sut = new CandidateHeap<ConstraintFactory>(variables, null!);

        variables[0].Activity = 5;
        variables[1].Activity = 8;
        variables[2].Activity = 2;
        variables[3].Activity = 7;
        variables[4].Activity = 9;

        sut.Heapify();

        Assert.Equal(5, sut.Count);
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

        var sut = new CandidateHeap<ConstraintFactory>(variables, null!);

        Assert.Equal(5, sut.Count);
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

        var sut = new CandidateHeap<ConstraintFactory>(variables, null!);

        Assert.Equal(5, sut.Count);
        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Equal(4, sut.Count);

        variables[4].Activity = 3;
        variables[4].Reason =  _constraintFactory.CreateInitialConstraint([variables[4].PositiveLiteral]);
        variables[4].DecisionLevel = 12;
        variables[4].Sense = true;
        sut.Enqueue([variables[4]]);
        Assert.Equal(5, sut.Count);
        Assert.Equal(0, variables[4].DecisionLevel);
        Assert.Null(variables[4].Reason);
        Assert.Null(variables[4].Sense);

        Assert.Equal(1, sut.Dequeue()!.Index);
        Assert.Equal(4, sut.Count);
        Assert.Equal(3, sut.Dequeue()!.Index);
        Assert.Equal(3, sut.Count);
        Assert.Equal(0, sut.Dequeue()!.Index);
        Assert.Equal(2, sut.Count);
        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Equal(1, sut.Count);
        Assert.Equal(2, sut.Dequeue()!.Index);
        Assert.Equal(0, sut.Count);
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

        var sut = new CandidateHeap<ConstraintFactory>(variables, null!);

        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Equal(1, sut.Dequeue()!.Index);
        variables[1].Activity = 10;
        variables[1].Reason = _constraintFactory.CreateInitialConstraint([variables[1].PositiveLiteral]);
        variables[1].DecisionLevel = 12;
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

        var sut = new CandidateHeap<ConstraintFactory>(variables, null!);

        variables[4].Activity = 3;
        variables[4].Reason = _constraintFactory.CreateInitialConstraint([variables[4].PositiveLiteral]);
        variables[4].DecisionLevel = 12;
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

        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var sut = new CandidateHeap<IConstraintFactory>(variables, constraintFactory.Object);

        variables[4].Activity = 12;
        variables[4].Reason = _constraintFactory.CreateInitialConstraint([variables[4].PositiveLiteral]);
        variables[4].DecisionLevel = 12;
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

        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var sut = new CandidateHeap<IConstraintFactory>(variables, constraintFactory.Object);

        variables[4].Activity = 1;
        variables[4].Reason = _constraintFactory.CreateInitialConstraint([variables[4].PositiveLiteral]);
        variables[4].Reason!.IsOmitted = true;
        constraintFactory.Setup(cf => cf.ReleaseConstraint(variables[4].Reason!));
        variables[4].DecisionLevel = 12;
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

        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var sut = new CandidateHeap<IConstraintFactory>(variables, constraintFactory.Object);

        variables[4].Activity = 5;
        variables[4].Reason = _constraintFactory.CreateInitialConstraint([variables[4].PositiveLiteral]);
        constraintFactory.Setup(cf => cf.ReleaseConstraint(variables[4].Reason!));
        variables[4].Reason!.IsOmitted = true;
        variables[4].DecisionLevel = 12;
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

        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var sut = new CandidateHeap<IConstraintFactory>(variables, constraintFactory.Object);

        variables[2].Activity = 3;
        variables[2].Reason = _constraintFactory.CreateInitialConstraint([variables[2].PositiveLiteral]);
        variables[2].DecisionLevel = 12;
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

        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var sut = new CandidateHeap<IConstraintFactory>(variables, constraintFactory.Object);

        variables[2].Activity = 9;
        variables[2].Reason = _constraintFactory.CreateInitialConstraint([variables[2].PositiveLiteral]);
        variables[2].DecisionLevel = 12;
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

    [Fact]
    public void Rescale_RescaleEnqueuedVariables()
    {
        var variables = Enumerable.Range(0, 5).Select(i => new Variable(i)).ToArray();
        variables[0].Activity = 5;
        variables[1].Activity = 8;
        variables[2].Activity = 2;
        variables[3].Activity = 7;
        variables[4].Activity = 9;

        var constraintFactory = new Mock<IConstraintFactory>(MockBehavior.Strict);
        var sut = new CandidateHeap<IConstraintFactory>(variables, constraintFactory.Object);

        Assert.Equal(4, sut.Dequeue()!.Index);
        Assert.Equal(1, sut.Dequeue()!.Index);

        sut.Rescale(2);

        variables[1].Activity = 4;
        sut.Enqueue([variables[1]]);
        Assert.Equal(1, sut.Dequeue()!.Index);
    }
}
