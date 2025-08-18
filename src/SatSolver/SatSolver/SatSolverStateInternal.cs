using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace Revo.SatSolver;

sealed class SatSolverStateInternal : SatSolverState
{
    ICandidateHeap? _candidateHeap;
    IVariableTrail? _trail;
    IVariablePropagator? _variablePropagator;

    IConflictDrivenConstraintLearner? _cdclProcessor;
    ICreateLearnedConstraints? _learnedConstraintCreator;
    IReduceLearnedConstraints? _learnedConstraintsReducer;
    IMinimizeConstraints? _constraintMinizer;
    RestartManager? _restartManager;

    IActivityManager? _activityManager;

    EmaTracker? _literalBlockDistanceTracker;
    PropagationRateTracker? _propagationRateTracker;

    public override ICandidateHeap CandidateHeap => _candidateHeap ??= new CandidateHeap(this);
    public override IVariableTrail VariableTrail => _trail ??= new VariableTrail(this);
    public override IVariablePropagator VariablePropagator => _variablePropagator ??= new VariablePropagator(this);
    public override IConflictDrivenConstraintLearner ConflictDrivenConstraintLearner => _cdclProcessor ??= new ConflictDrivenConstraintLearner(this);

    public override IActivityManager ActivityManager => _activityManager ??= new ActivityManager(this);
    public override EmaTracker LiteralBlockDistanceTracker => _literalBlockDistanceTracker ??= new EmaTracker(Options.LiteralBlockDistanceTracking.RecentCount, Options.LiteralBlockDistanceTracking.Decay);
    public override PropagationRateTracker PropagationRateTracker => _propagationRateTracker ??= new PropagationRateTracker(Options.PropagationRateTracking.ConflictInterval, Options.PropagationRateTracking.SampleSize, Options.PropagationRateTracking.Decay);

    public override RestartManager RestartManager => _restartManager ??= new RestartManager(this);

    public override ICreateLearnedConstraints LearnedConstraintCreator => _learnedConstraintCreator ??= new LearnedConstraintCreator(this);
    public override IReduceLearnedConstraints LearnedConstraintsReducer => _learnedConstraintsReducer ??= new LearnedConstraintsReducer(this);
    public override IMinimizeConstraints ConstraintMinimizer => _constraintMinizer ??= new ConstraintMinimizer(this);

    internal SatSolverStateInternal(SatSolverOptions options, int originalConstraintCount, Variable[] variables, Queue<(ConstraintLiteral UnitLiteral, Constraint Reason)> unitsToPropagate, CancellationToken cancellationToken) 
        : base(options, originalConstraintCount, variables, unitsToPropagate, cancellationToken)
    { }
}