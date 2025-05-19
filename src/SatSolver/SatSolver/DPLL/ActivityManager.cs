using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.DPLL;
sealed class ActivityManager(Variable[] variables, List<Constraint> learnedConstraints, double variableActivityDecay, double constraintActivityDecay, ICandidateHeap candidateHeap) : IActivityManager
{
    const double _rescaleLimit = 1e100;

    readonly Variable[] _variables = variables;
    readonly List<Constraint> _learnedConstraints = learnedConstraints;
    readonly double _variableActivityDecay = variableActivityDecay;
    readonly double _constraintActivityDecay = constraintActivityDecay;
    readonly ICandidateHeap _candidateHeap = candidateHeap;

    double _constraintActivityIncrement = 1, _variableActivityIncrement = 1;

    public double ConstraintActivityIncrement => _constraintActivityIncrement;
    public double VariableActivityIncrement => _variableActivityIncrement;

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
    public void DecayConstraintActivity() => _constraintActivityIncrement /= _constraintActivityDecay;

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
    void RescaleVariableActivity()
    {
        var variables = _variables;
        for (var i = 0; i < variables.Length; i++)
            variables[i].Activity /= _rescaleLimit;
        _candidateHeap.Rescale(_rescaleLimit);
        _variableActivityIncrement /= _rescaleLimit;
    }
}
