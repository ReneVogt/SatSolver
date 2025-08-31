using Moq;
using Revo.SatSolver.DataStructures;

namespace SatSolverTests.DataStructures;

public sealed class VariableTrailTests
{
    sealed class TestHeap : ICandidateHeap
    {
        public Queue<Variable> Enqueued { get; } = [];
        public int RescaleCalls { get; set; }

        public void Heapify() => throw new NotImplementedException();
        public int Count => throw new NotImplementedException();
        public Variable? Dequeue() => Enqueued.Dequeue();
        public void Enqueue(Span<Variable> variables)
        {
            foreach (var variable in variables) Enqueued.Enqueue(variable);
        }
        public void Rescale(double scaleLimit) => RescaleCalls++;
    }

    [Fact]
    public void JumpBack_CurrentLevel_NoChanges()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();
        var heap = new TestHeap();
        var sut = new VariableTrail<TestHeap>(heap, variables.Length);

        sut.Push(true);
        Assert.Equal(1, sut.DecisionLevel);
        sut.Add(variables[0]);
        sut.Add(variables[1]);
        sut.Add(variables[2]);
        Assert.Equal(3, sut.Count);
        Assert.Equal(1, variables[0].DecisionLevel);
        Assert.Equal(1, variables[1].DecisionLevel);
        Assert.Equal(1, variables[2].DecisionLevel);

        sut.Push(true);
        Assert.Equal(2, sut.DecisionLevel);
        sut.Add(variables[3]);
        Assert.Equal(4, sut.Count);
        Assert.Equal(2, variables[3].DecisionLevel);

        sut.Push(true);
        Assert.Equal(3, sut.DecisionLevel);
        sut.Add(variables[4]);
        sut.Add(variables[5]);
        Assert.Equal(6, sut.Count);
        Assert.Equal(3, variables[4].DecisionLevel);
        Assert.Equal(3, variables[5].DecisionLevel);

        sut.Push(true);

        sut.JumpBack(sut.DecisionLevel);

        Assert.Empty(heap.Enqueued);

        Assert.Equal(4, sut.DecisionLevel);
        sut.Add(variables[6]);
        sut.Add(variables[7]);
        sut.Add(variables[8]);
        sut.Add(variables[9]);
        Assert.Equal(10, sut.Count);
        Assert.Equal(4, variables[6].DecisionLevel);
        Assert.Equal(4, variables[7].DecisionLevel);
        Assert.Equal(4, variables[8].DecisionLevel);
        Assert.Equal(4, variables[9].DecisionLevel);

        var trail = Enumerable.Range(0, 10).Select(i => sut[i].Index);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9], trail);
        Assert.Equal(10, sut.Count);
    }
    [Fact]
    public void AddFourDecisionLevels_JumpBackTwo()
    {
        var variables = Enumerable.Range(0, 10).Select(i =>  new Variable(i)).ToArray();
        var heap = new TestHeap();
        var sut = new VariableTrail<TestHeap>(heap, variables.Length);

        sut.Push(true);
        Assert.Equal(1, sut.DecisionLevel);
        sut.Add(variables[0]);
        sut.Add(variables[1]);
        sut.Add(variables[2]);
        Assert.Equal(3, sut.Count);
        Assert.Equal(1, variables[0].DecisionLevel);
        Assert.Equal(1, variables[1].DecisionLevel);
        Assert.Equal(1, variables[2].DecisionLevel);

        sut.Push(true);
        Assert.Equal(2, sut.DecisionLevel);
        sut.Add(variables[3]);
        Assert.Equal(4, sut.Count);
        Assert.Equal(2, variables[3].DecisionLevel);

        sut.Push(true);
        Assert.Equal(3, sut.DecisionLevel);
        sut.Add(variables[4]);
        sut.Add(variables[5]);
        Assert.Equal(6, sut.Count);
        Assert.Equal(3, variables[4].DecisionLevel);
        Assert.Equal(3, variables[5].DecisionLevel);

        sut.Push(true);
        Assert.Equal(4, sut.DecisionLevel);
        sut.Add(variables[6]);
        sut.Add(variables[7]);
        sut.Add(variables[8]);
        sut.Add(variables[9]);
        Assert.Equal(10, sut.Count);
        Assert.Equal(4, variables[6].DecisionLevel);
        Assert.Equal(4, variables[7].DecisionLevel);
        Assert.Equal(4, variables[8].DecisionLevel);
        Assert.Equal(4, variables[9].DecisionLevel);

        var trail = Enumerable.Range(0, 10).Select(i => sut[i].Index);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9], trail);
        Assert.Equal(10, sut.Count);

        sut.JumpBack(2);
        Assert.Equal(4, sut.Count);
        Assert.Equal(2, sut.DecisionLevel);

        Assert.Equal([4, 5, 6, 7, 8, 9], heap.Enqueued.Select(v => v.Index));
    }
    [Fact]
    public void AddFourDecisionLevels_BacktrackTwo()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();
        var heap = new TestHeap();
        var sut = new VariableTrail<TestHeap>(heap, variables.Length);

        sut.Push(true);
        Assert.Equal(1, sut.DecisionLevel);
        sut.Add(variables[0]);
        sut.Add(variables[1]);
        sut.Add(variables[2]);
        Assert.Equal(3, sut.Count);
        Assert.Equal(1, variables[0].DecisionLevel);
        Assert.Equal(1, variables[1].DecisionLevel);
        Assert.Equal(1, variables[2].DecisionLevel);

        sut.Push(true);
        Assert.Equal(2, sut.DecisionLevel);
        variables[3].Sense = false;
        sut.Add(variables[3]);
        Assert.Equal(4, sut.Count);
        Assert.Equal(2, variables[3].DecisionLevel);

        sut.Push(false);
        Assert.Equal(3, sut.DecisionLevel);
        sut.Add(variables[4]);
        sut.Add(variables[5]);
        Assert.Equal(6, sut.Count);
        Assert.Equal(3, variables[4].DecisionLevel);
        Assert.Equal(3, variables[5].DecisionLevel);

        sut.Push(false);
        Assert.Equal(4, sut.DecisionLevel);
        sut.Add(variables[6]);
        sut.Add(variables[7]);
        sut.Add(variables[8]);
        sut.Add(variables[9]);
        Assert.Equal(10, sut.Count);
        Assert.Equal(4, variables[6].DecisionLevel);
        Assert.Equal(4, variables[7].DecisionLevel);
        Assert.Equal(4, variables[8].DecisionLevel);
        Assert.Equal(4, variables[9].DecisionLevel);

        var trail = Enumerable.Range(0, 10).Select(i => sut[i].Index);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9], trail);
        Assert.Equal(10, sut.Count);

        var (candidate, sense) = sut.Backtrack();
        Assert.Equal(3, sut.Count);
        Assert.Equal(1, sut.DecisionLevel);
        Assert.Equal([3, 4, 5, 6, 7, 8, 9], heap.Enqueued.Select(v => v.Index));
        Assert.Equal(3, candidate!.Index);
        Assert.True(sense);
    }
    [Fact]
    public void Backtrack_NoFirstTry_Null()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();
        var heap = new TestHeap();
        var sut = new VariableTrail<TestHeap>(heap, variables.Length);

        sut.Push(false);
        Assert.Equal(1, sut.DecisionLevel);
        sut.Add(variables[0]);
        sut.Add(variables[1]);
        sut.Add(variables[2]);
        Assert.Equal(3, sut.Count);
        Assert.Equal(1, variables[0].DecisionLevel);
        Assert.Equal(1, variables[1].DecisionLevel);
        Assert.Equal(1, variables[2].DecisionLevel);

        sut.Push(false);
        Assert.Equal(2, sut.DecisionLevel);
        variables[3].Sense = false;
        sut.Add(variables[3]);
        Assert.Equal(4, sut.Count);
        Assert.Equal(2, variables[3].DecisionLevel);

        sut.Push(false);
        Assert.Equal(3, sut.DecisionLevel);
        sut.Add(variables[4]);
        sut.Add(variables[5]);
        Assert.Equal(6, sut.Count);
        Assert.Equal(3, variables[4].DecisionLevel);
        Assert.Equal(3, variables[5].DecisionLevel);

        sut.Push(false);
        Assert.Equal(4, sut.DecisionLevel);
        sut.Add(variables[6]);
        sut.Add(variables[7]);
        sut.Add(variables[8]);
        sut.Add(variables[9]);
        Assert.Equal(10, sut.Count);
        Assert.Equal(4, variables[6].DecisionLevel);
        Assert.Equal(4, variables[7].DecisionLevel);
        Assert.Equal(4, variables[8].DecisionLevel);
        Assert.Equal(4, variables[9].DecisionLevel);

        var trail = Enumerable.Range(0, 10).Select(i => sut[i].Index);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9], trail);
        Assert.Equal(10, sut.Count);

        (var candidate, _) = sut.Backtrack();
        Assert.Equal(0, sut.Count);
        Assert.Equal(0, sut.DecisionLevel);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9], heap.Enqueued.Select(v => v.Index));
        Assert.Null(candidate);
    }
    [Fact]
    public void Reset_ResetsAllVariables()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();
        var heap = new TestHeap();
        var sut = new VariableTrail<TestHeap>(heap, variables.Length);

        sut.Push(true);
        Assert.Equal(1, sut.DecisionLevel);
        sut.Add(variables[0]);
        sut.Add(variables[1]);
        sut.Add(variables[2]);
        Assert.Equal(3, sut.Count);
        Assert.Equal(1, variables[0].DecisionLevel);
        Assert.Equal(1, variables[1].DecisionLevel);
        Assert.Equal(1, variables[2].DecisionLevel);

        sut.Push(true);
        Assert.Equal(2, sut.DecisionLevel);
        variables[3].Sense = false;
        sut.Add(variables[3]);
        Assert.Equal(4, sut.Count);
        Assert.Equal(2, variables[3].DecisionLevel);

        sut.Push(false);
        Assert.Equal(3, sut.DecisionLevel);
        sut.Add(variables[4]);
        sut.Add(variables[5]);
        Assert.Equal(6, sut.Count);
        Assert.Equal(3, variables[4].DecisionLevel);
        Assert.Equal(3, variables[5].DecisionLevel);

        sut.Push(false);
        Assert.Equal(4, sut.DecisionLevel);
        sut.Add(variables[6]);
        sut.Add(variables[7]);
        sut.Add(variables[8]);
        sut.Add(variables[9]);
        Assert.Equal(10, sut.Count);
        Assert.Equal(4, variables[6].DecisionLevel);
        Assert.Equal(4, variables[7].DecisionLevel);
        Assert.Equal(4, variables[8].DecisionLevel);
        Assert.Equal(4, variables[9].DecisionLevel);

        var trail = Enumerable.Range(0, 10).Select(i => sut[i].Index);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9], trail);
        Assert.Equal(10, sut.Count);

        sut.Reset();
        Assert.Equal(0, sut.Count);
        Assert.Equal(0, sut.DecisionLevel);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9], heap.Enqueued.Select(v => v.Index));
    }
    [Fact]
    public void Clear_DoesNotResetVariables()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();
        var heap = new TestHeap();
        var sut = new VariableTrail<TestHeap>(heap, variables.Length);

        sut.Push(true);
        Assert.Equal(1, sut.DecisionLevel);
        sut.Add(variables[0]);
        sut.Add(variables[1]);
        sut.Add(variables[2]);
        Assert.Equal(3, sut.Count);
        Assert.Equal(1, variables[0].DecisionLevel);
        Assert.Equal(1, variables[1].DecisionLevel);
        Assert.Equal(1, variables[2].DecisionLevel);

        sut.Push(true);
        Assert.Equal(2, sut.DecisionLevel);
        variables[3].Sense = false;
        sut.Add(variables[3]);
        Assert.Equal(4, sut.Count);
        Assert.Equal(2, variables[3].DecisionLevel);

        sut.Push(false);
        Assert.Equal(3, sut.DecisionLevel);
        sut.Add(variables[4]);
        sut.Add(variables[5]);
        Assert.Equal(6, sut.Count);
        Assert.Equal(3, variables[4].DecisionLevel);
        Assert.Equal(3, variables[5].DecisionLevel);

        sut.Push(false);
        Assert.Equal(4, sut.DecisionLevel);
        sut.Add(variables[6]);
        sut.Add(variables[7]);
        sut.Add(variables[8]);
        sut.Add(variables[9]);
        Assert.Equal(10, sut.Count);
        Assert.Equal(4, variables[6].DecisionLevel);
        Assert.Equal(4, variables[7].DecisionLevel);
        Assert.Equal(4, variables[8].DecisionLevel);
        Assert.Equal(4, variables[9].DecisionLevel);

        var trail = Enumerable.Range(0, 10).Select(i => sut[i].Index);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9], trail);
        Assert.Equal(10, sut.Count);

    }

    [Fact]
    public void StartIndexOfCurrentDecisionLevel()
    {
        var variables = Enumerable.Range(0, 10).Select(i => new Variable(i)).ToArray();
        var heap = new TestHeap();
        var sut = new VariableTrail<TestHeap>(heap, variables.Length);

        foreach (var v in variables) v.Sense = true;

        Assert.Equal(-1, sut.StartIndexOfCurrentDecisionLevel);
        
        sut.Push(true);
        Assert.Equal(0, sut.StartIndexOfCurrentDecisionLevel);
        sut.Add(variables[0]);
        sut.Add(variables[1]);
        Assert.Equal(0, sut.StartIndexOfCurrentDecisionLevel);

        sut.Push(true);
        Assert.Equal(2, sut.StartIndexOfCurrentDecisionLevel);
        sut.Add(variables[2]);
        sut.Add(variables[3]);
        sut.Add(variables[4]);
        Assert.Equal(2, sut.StartIndexOfCurrentDecisionLevel);

        sut.Push(true);
        Assert.Equal(5, sut.StartIndexOfCurrentDecisionLevel);
        sut.Add(variables[5]);
        Assert.Equal(5, sut.StartIndexOfCurrentDecisionLevel);

        sut.Backtrack();
        Assert.Equal(2, sut.StartIndexOfCurrentDecisionLevel);
        sut.Backtrack();
        Assert.Equal(0, sut.StartIndexOfCurrentDecisionLevel);
    }
}
