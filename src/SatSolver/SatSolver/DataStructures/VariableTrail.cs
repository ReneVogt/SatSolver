using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Revo.SatSolver.DataStructures;

sealed class VariableTrail(int size, ICandidateHeap candidateHeap) : IVariableTrail
{
    readonly ICandidateHeap _candidateHeap = candidateHeap;
    readonly Variable[] _trail = new Variable[size];
    readonly Stack<(int TrailIndex, bool FirstTryOfCandidate)> _decisionLevels = new(size);

    int _trailSize;

    public int Count => _trailSize;
    public int DecisionLevel => _decisionLevels.Count;

    public Variable this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _trail[index];
    }

    public void Add(Variable variable)
    {
        _trail[_trailSize++] = variable;
        variable.DecisionLevel = DecisionLevel;
    }
    public void Push(bool firstTryOfCandidate) => _decisionLevels.Push((_trailSize, firstTryOfCandidate));

    public void JumpBack(int level)
    {
        Debug.WriteLine($"[{DecisionLevel}] Jumping back to level {level}.");
        var index = 0;
        while (_decisionLevels.Count > level)
            (index, _) = _decisionLevels.Pop();

        ResetVariableTrail(index);
    }
    public (Variable? candidate, bool sense) Backtrack()
    {
        var first = false;
        var index = -1;
        while (_decisionLevels.Count > 0 && !first) (index, first) = _decisionLevels.Pop();
        if (!first)
        {
            ResetVariableTrail(0);
            return (null, true);
        }

        var variable = _trail[index];
        var sense = !variable.Sense!.Value;

        ResetVariableTrail(index);
        return (variable, sense);

    }
    public void Reset()
    {
        _decisionLevels.Clear();
        ResetVariableTrail(0);
    }
    public void Clear()
    {
        _decisionLevels.Clear();
        _trailSize = 0;
    }

    void ResetVariableTrail(int targetLevelStart)
    {
        _candidateHeap.Enqueue(_trail.AsSpan(targetLevelStart.._trailSize));
        _trailSize = targetLevelStart;
    }
}
