using Revo.SatSolver.DataStructures;

namespace SatSolverTests.Stubs;

sealed class TestCandidateHeap : ICandidateHeap
{
    public List<Variable> Enqueued { get; } = [];
    public int RescaleCalls { get; set; }

    public Variable? Dequeue() => throw new NotImplementedException();
    public void Enqueue(Span<Variable> variables) => Enqueued.AddRange(variables);
    public void Rescale(double scaleLimit) => RescaleCalls++;
}
