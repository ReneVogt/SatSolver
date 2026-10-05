namespace Revo.SatSolver.DataStructures;

sealed class VariableTrail(ICandidateHeap _candidateHeap, int _capacity) : IVariableTrail
{
    readonly Variable[] _trail = new Variable[_capacity];
    readonly Stack<int> _decisionLevels = new(_capacity);

    int _trailSize;

    public int Count => _trailSize;
    public int DecisionLevel => _decisionLevels.Count;
    public int StartIndexOfCurrentDecisionLevel => _decisionLevels.TryPeek(out var trailIndex) ? trailIndex : -1; 

    public Variable this[int index]
    {
        get => _trail[index];
    }

    public void Add(Variable variable)
    {
        _trail[_trailSize++] = variable;
        variable.DecisionLevel = DecisionLevel;
    }
    public void Push() => _decisionLevels.Push(_trailSize);

    public void JumpBack(int level)
    {
        Statistics.LogBackJump(_decisionLevels.Count, level);
        var index = Count;
        while (_decisionLevels.Count > level)
            index = _decisionLevels.Pop();

        ResetVariableTrail(index);
    }
    public void Reset()
    {
        _decisionLevels.Clear();
        ResetVariableTrail(0);
    }

    void ResetVariableTrail(int targetLevelStart)
    {
        _candidateHeap.Enqueue(_trail.AsSpan(targetLevelStart.._trailSize));
        _trailSize = targetLevelStart;
    }
}
