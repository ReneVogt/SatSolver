using Revo.SatSolver.DataStructures;
using System.Runtime.CompilerServices;

namespace Revo.SatSolver.Tools;
sealed class ActivityManager<TCandidateHeap>(Variable[] _variables, List<Constraint> _learnedConstraints, TCandidateHeap _candidateHeap, SatSolverOptions _options) : IManageActivities
    where TCandidateHeap : ICandidateHeap
{
    const double _rescaleLimit = 1e100;

    readonly double _variableActivityDecay = _options.VariableActivityDecayFactor;
    readonly double _constraintActivityDecay = _options.ConstraintActivityDecayFactor;

    double _constraintActivityIncrement = 1, _variableActivityIncrement = 1;

    public double ConstraintActivityIncrement => _constraintActivityIncrement;
    public double VariableActivityIncrement => _variableActivityIncrement;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncreaseConstraintActivity(Constraint constraint, double factor = 1)
    {
        if (!constraint.IsTracked) return;

        constraint.Activity += _constraintActivityIncrement * factor;
        if (constraint.Activity < _rescaleLimit) return;

        var learnedConstraints = _learnedConstraints;
        for (var i = 0; i<learnedConstraints.Count; i++)
            learnedConstraints[i].Activity /= _rescaleLimit;
        _constraintActivityIncrement /= _rescaleLimit;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DecayConstraintActivity() => _constraintActivityIncrement /= _constraintActivityDecay;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncreaseVariableActivity(Constraint constraint)
    {
        var literals = constraint.Literals;
        for (var i = 0; i<literals.Length; i++)
        {
            var activity = literals[i].Variable.Activity += _variableActivityIncrement;
            if (activity < _rescaleLimit) continue;
            RescaleVariableActivity();
        }
        _variableActivityIncrement /= _variableActivityDecay;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void RescaleVariableActivity()
    {
        var variables = _variables;
        for (var i = 0; i < variables.Length; i++)
            variables[i].Activity /= _rescaleLimit;
        _candidateHeap.Rescale(_rescaleLimit);
        _variableActivityIncrement /= _rescaleLimit;
    }
}
