using Revo.SatSolver.Helpers;

namespace SatSolverTests.Helpers;

public sealed class CandidateHeapTests
{
    [Fact]
    public void InitializedAndEnqueued_CorrectSequence()
    {
        var startupSequence = new double[] { 3, 2, 7, 8, 5, 12 };
        var expectedVariables = new[] { 5, 3, 2, 4, 0, 1 };
        var sut = new CandidateHeap(startupSequence);

        Assert.Equal(startupSequence.Length, sut.Count);
        foreach (var v in expectedVariables)
            Assert.Equal(v, sut.Dequeue());

        Assert.Equal(0, sut.Count);

        for (var v = 0; v <startupSequence.Length; v++)
            sut.Enqueue(v, startupSequence[v]);
        Assert.Equal(startupSequence.Length, sut.Count);
        foreach (var v in expectedVariables)
            Assert.Equal(v, sut.Dequeue());
    }
    [Fact]
    public void EnqueueingExisting_Works()
    {
        var startupSequence = new double[] { 3, 2, 7, 8, 5, 12 };
        var sut = new CandidateHeap(startupSequence);
        sut.Enqueue(0, 4);
        sut.Enqueue(3, 6);
        // { 4, 2, 7, 6, 5, 12 };
        var expectedVariables = new[] { 5, 2, 3, 4, 0, 1 };

        Assert.Equal(startupSequence.Length, sut.Count);
        foreach (var v in expectedVariables)
            Assert.Equal(v, sut.Dequeue());

        Assert.Equal(0, sut.Count);
    }
}
