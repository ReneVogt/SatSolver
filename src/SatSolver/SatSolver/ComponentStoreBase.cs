using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;
using System.Diagnostics.CodeAnalysis;

namespace Revo.SatSolver;

[ExcludeFromCodeCoverage]
abstract class ComponentStoreBase
{
    public SatSolverOptions Options { get; }
    public Variable[] Variables { get; }
    public ConstraintLiteral[] Literals { get; }
    public UnitPropagationQueue UnitPropagationQueue { get; } = [];
    public List<Constraint> LearnedConstraints { get; } = [];
    public Dictionary<long, Constraint> AdditionalConstraints { get; } = [];
    public CancellationToken CancellationToken { get; }

    public abstract IPreProcessor PreProcessor { get; }
    public abstract IConstraintFactory ConstraintFactory { get; }
    public abstract ICandidateHeap CandidateHeap { get; }
    public abstract IVariableTrail VariableTrail { get; }
    public abstract IPropagateVariables VariablePropagator { get; }
    public abstract IHandleConflicts ConflictHandler { get; }
    public abstract ICreateLearnedConstraints LearnedConstraintCreator { get; }
    public abstract IReduceLearnedConstraints LearnedConstraintsReducer { get; }
    public abstract IMinimizeConstraints ConstraintMinimizer { get; }
    public abstract IManageActivities ActivityManager { get; }
    public abstract ITrackLiteralBlockDistance LiteralBlockDistanceTracker { get; }
    public abstract ITrackPropagationRate PropagationRateTracker { get; }
    public abstract IManageRestart RestartManager { get; }

    private protected ComponentStoreBase(SatSolverOptions options, int variableCount, CancellationToken cancellationToken)
    {
        Options = options;
        Variables = [.. Enumerable.Range(0, variableCount).Select(i => new Variable(i))];
        Literals = new ConstraintLiteral[variableCount<<1];
        for (var i = 0; i<variableCount; i++)
        {
            Literals[i<<1] = Variables[i].PositiveLiteral;
            Literals[(i<<1)+1] = Variables[i].NegativeLiteral;
        }
        CancellationToken = cancellationToken;
    }
}
