using Revo.SatSolver.DataStructures;

namespace SatSolverTests.Stubs;

sealed class TestVariableTrail : IVariableTrail
{
    public List<Variable> AddedVariables { get; } = [];
    public Variable this[int index] => throw new NotImplementedException();
    public int Count => throw new NotImplementedException();
    public int DecisionLevel => 0;
    public int StartIndexOfCurrentDecisionLevel => throw new NotImplementedException();
    public void Add(Variable variable) => AddedVariables.Add(variable);
    public (Variable? candidate, bool sense) Backtrack() => throw new NotImplementedException();
    public void Clear() => throw new NotImplementedException();
    public void JumpBack(int level) => throw new NotImplementedException();
    public void Push(bool firstTryOfCandidate) => throw new NotImplementedException();
    public void Reset() => throw new NotImplementedException();
}
