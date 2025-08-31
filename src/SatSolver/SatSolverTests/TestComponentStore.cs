using Revo.SatSolver;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace SatSolverTests;
sealed class TestComponentStore(SatSolverOptions options, int variableCount, Func<string, object> getter, CancellationToken cancellationToken = default) : ComponentStoreBase(options, variableCount, cancellationToken)
{
    public override IPreProcessor PreProcessor => (IPreProcessor)getter(nameof(PreProcessor));
    public override IConstraintFactory ConstraintFactory => (IConstraintFactory)getter(nameof(ConstraintFactory));
    public override ICandidateHeap CandidateHeap => (ICandidateHeap)getter(nameof(CandidateHeap));
    public override IVariableTrail VariableTrail => (IVariableTrail)getter(nameof(VariableTrail));
    public override IPropagateVariables VariablePropagator => (IPropagateVariables)getter(nameof(VariablePropagator));
    public override IHandleConflicts ConflictHandler => (IHandleConflicts)getter(nameof(ConflictHandler));
    public override ICreateLearnedConstraints LearnedConstraintCreator => (ICreateLearnedConstraints)getter(nameof(LearnedConstraintCreator));
    public override IReduceLearnedConstraints LearnedConstraintsReducer => (IReduceLearnedConstraints)getter(nameof(LearnedConstraintsReducer));
    public override IMinimizeConstraints ConstraintMinimizer => (IMinimizeConstraints)getter(nameof(ConstraintMinimizer));
    public override IManageActivities ActivityManager => (IManageActivities)getter(nameof(ActivityManager));
    public override ITrackLiteralBlockDistance LiteralBlockDistanceTracker => (ITrackLiteralBlockDistance)getter(nameof(LiteralBlockDistanceTracker));
    public override ITrackPropagationRate PropagationRateTracker => (ITrackPropagationRate)getter(nameof(PropagationRateTracker));
    public override IManageRestart RestartManager => (IManageRestart)getter(nameof(RestartManager));
}
