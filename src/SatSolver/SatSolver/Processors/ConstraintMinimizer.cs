using Revo.SatSolver.DataStructures;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Revo.SatSolver.Processors;

sealed class ConstraintMinimizer : IMinimizeConstraints
{
    const int _maxReasonSize = 12;

    readonly StampArray _seen = [];
    readonly StampArray _redundant = [];
    readonly StampArray _notRedundant = [];
    readonly Stack<(Variable Variable, int Index)> _stack = [];

    int[] _constraintBuffer = new int[1024];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MinimizeConstraint(StampArray constraint, int decisionLevel, ConstraintLiteral[] knownLiterals)
    {
        Statistics.StartConstraintMinimization(constraint.Count);

        int maxStackSize = 64 * constraint.Count;
        int visitLimit = Math.Max(3000, 20 * constraint.Count);
        _redundant.Clear();
        _notRedundant.Clear();

        CheckBufferSize(constraint.Count);
        var i = 0;
        foreach (var index in constraint)
            _constraintBuffer[i++] = index;
        for (i = 0; i< constraint.Count; i++)
        {
            var index = _constraintBuffer[i];
            if (IsRedundant(index))
                constraint.Remove(index);
        }

        Statistics.FinishConstraintMinimization(constraint.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsRedundant(int literalIndex)
        {
            var learnedLiteral = knownLiterals[literalIndex];
            var learnedVariable = learnedLiteral.Variable;
            if (learnedVariable.DecisionLevel == decisionLevel) return false;

            _stack.Clear();
            _stack.Push((learnedVariable, -1));
            _seen.Clear();
            var visitBudget = 0;

            while(_stack.Count > 0)            
            {
                if (++visitBudget > visitLimit) return false;

                var (variable, index) = _stack.Pop();
                if (variable.DecisionLevel == 0)
                {
                    _redundant.Add(variable.Index);
                    continue;
                }
                if (_redundant.Contains(variable.Index)) continue;
                if (_notRedundant.Contains(variable.Index)) return false;

                if (index < 0)
                { 
                    var r = variable.Reason;
                    if (r is null || r.Literals.Length > _maxReasonSize)
                    {
                        _notRedundant.Add(variable.Index);
                        return false;
                    }

                    _stack.Push((variable, 0));
                    if (_stack.Count > maxStackSize) return false;
                    continue;
                }

                var reason = variable.Reason!;
                if (index >= reason.Literals.Length)
                {
                    _redundant.Add(variable.Index);
                    continue;
                }

                var reasonLiteral = reason.Literals[index++];
                _stack.Push((variable, index));
                if (_stack.Count > maxStackSize) return false;

                var reasonVariable = reasonLiteral.Variable;
                if (variable == reasonVariable) continue;
                if (reasonVariable.DecisionLevel == 0) continue;
                if (constraint.Contains(reasonLiteral.StampIndex) || constraint.Contains(reasonLiteral.StampIndex^1)) continue;
                if (_redundant.Contains(reasonVariable.Index)) continue;
                if (_notRedundant.Contains(reasonVariable.Index))
                {
                    _notRedundant.Add(variable.Index);
                    return false;
                }
                if (!_seen.Add(reasonLiteral.StampIndex)) continue;
                _stack.Push((reasonVariable, -1));
                if (_stack.Count > maxStackSize) return false;
            }

            return true;
        }
    }

    [ExcludeFromCodeCoverage]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void CheckBufferSize(int required)
    {
        if (required > _constraintBuffer.Length)
            Array.Resize(ref _constraintBuffer, required << 1);
    }
}
