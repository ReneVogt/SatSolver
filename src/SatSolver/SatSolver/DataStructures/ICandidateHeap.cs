namespace Revo.SatSolver.DataStructures;

interface ICandidateHeap
{
    Variable? Dequeue();
    void Enqueue(Span<Variable> variables);
    void Rescale(double scaleLimit);
}