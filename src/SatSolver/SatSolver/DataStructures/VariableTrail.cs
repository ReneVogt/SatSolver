using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Revo.SatSolver.DataStructures;

sealed class VariableTrail<TCandidateHeap>(ICandidateHeap candidateHeap, int _capacity) : IVariableTrail where TCandidateHeap : ICandidateHeap
{
    readonly TCandidateHeap _candidateHeap = (TCandidateHeap)candidateHeap;
    readonly Variable[] _trail = new Variable[_capacity];
    readonly Stack<(int TrailIndex, bool FirstTryOfCandidate)> _decisionLevels = new(_capacity);

    int _trailSize;

    public int Count => _trailSize;
    public int DecisionLevel => _decisionLevels.Count;
    public int StartIndexOfCurrentDecisionLevel => _decisionLevels.TryPeek(out var l) ? l.TrailIndex : -1; 

    public Variable this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _trail[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Variable variable)
    {
        _trail[_trailSize++] = variable;
        variable.DecisionLevel = DecisionLevel;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(bool firstTryOfCandidate = true) => _decisionLevels.Push((_trailSize, firstTryOfCandidate));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void JumpBack(int level)
    {
        Debug.WriteLine($"[{DecisionLevel}] Jumping back to level {level}.");
        var index = Count;
        while (_decisionLevels.Count > level)
            (index, _) = _decisionLevels.Pop();

        ResetVariableTrail(index);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _decisionLevels.Clear();
        ResetVariableTrail(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ResetVariableTrail(int targetLevelStart)
    {
        _candidateHeap.Enqueue(_trail.AsSpan(targetLevelStart.._trailSize));
        _trailSize = targetLevelStart;
    }
}
