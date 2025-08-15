namespace Revo.SatSolver.DataStructures;

interface IVariableTrail
{
    Variable this[int index] { get; }

    int Count { get; }
    int DecisionLevel { get; }

    int StartIndexOfCurrentDecisionLevel { get; }

    void Add(Variable variable);
    (Variable? candidate, bool sense) Backtrack();
    void Clear();
    void JumpBack(int level);
    void Push(bool firstTryOfCandidate = true);
    void Reset();
}