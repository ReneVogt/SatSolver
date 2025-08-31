namespace Revo.SatSolver.DataStructures;

interface ICandidateHeap
{
    int Count { get; }
    Variable? Dequeue();
    void Enqueue(Span<Variable> variables);
    void Heapify();
    void Rescale(double scaleLimit);
}