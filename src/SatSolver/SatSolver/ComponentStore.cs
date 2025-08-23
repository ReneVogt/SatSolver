using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace Revo.SatSolver;

sealed record ComponentStore(
    SatSolverOptions Options,
    int OriginalConstraintCount,
    Variable[] Variables,
    UnitPropagationQueue UnitPropagationQueue,
    ICandidateHeap CandidateHeap,
    IVariableTrail VariableTrail,
    IPropagateVariables VariablePropagator,
    IHandleConflicts ConflictHandler,
    ICreateLearnedConstraints LearnedConstraintCreator,
    IReduceLearnedConstraints LearnedConstraintsReducer,
    IMinimizeConstraints ConstraintMinimizer,
    List<Constraint> LearnedConstraints,
    IManageActivities ActivityManager,
    ITrackLiteralBlockDistance LiteralBlockDistanceTracker,
    ITrackPropagationRate PropagationRateTracker,
    IManageRestart RestartManager,
    CancellationToken CancellationToken);