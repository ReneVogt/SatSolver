namespace Revo.SatSolver.DataStructures;

interface IVariableTrail
{
    Variable this[int index] { get; }

    int Count { get; }
    int DecisionLevel { get; }

    int StartIndexOfCurrentDecisionLevel { get; }

    void Add(Variable variable);
    void JumpBack(int level);
    void Push();
    void Reset();
}