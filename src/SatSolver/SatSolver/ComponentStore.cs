using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;
using System.Diagnostics.CodeAnalysis;

namespace Revo.SatSolver;

[ExcludeFromCodeCoverage]
sealed class ComponentStore : ComponentStoreBase
{
    public override IPreProcessor PreProcessor { get; }
    public override IConstraintFactory ConstraintFactory { get; }
    public override ICandidateHeap CandidateHeap { get; }
    public override IVariableTrail VariableTrail { get; }
    public override IPropagateVariables VariablePropagator { get; }
    public override IHandleConflicts ConflictHandler { get; }
    public override ICreateLearnedConstraints LearnedConstraintCreator { get; }
    public override IReduceLearnedConstraints LearnedConstraintsReducer { get; }
    public override IMinimizeConstraints ConstraintMinimizer { get; }
    public override IManageActivities ActivityManager { get; }
    public override ITrackLiteralBlockDistance LiteralBlockDistanceTracker { get; }
    public override ITrackPropagationRate PropagationRateTracker { get; }
    public override IManageRestart RestartManager { get; }

    public ComponentStore(SatSolverOptions options, Problem problem, CancellationToken cancellationToken) : base(options, problem.NumberOfLiterals, cancellationToken)
    {
        var propagationTrackingOptions = options.PropagationRateTracking;
        PropagationRateTracker = new PropagationRateTracker(propagationTrackingOptions.LocalHalflife, propagationTrackingOptions.GlobalHalflife, propagationTrackingOptions.Threshold, propagationTrackingOptions.HoldForConflicts, propagationTrackingOptions.CoolDownConflicts);
        var lbdTrackingOptions = options.LiteralBlockDistanceTracking;
        LiteralBlockDistanceTracker = new LiteralBlockDistanceTracker(lbdTrackingOptions.LocalHalflife, lbdTrackingOptions.GlobalHalflife, lbdTrackingOptions.Threshold, lbdTrackingOptions.HoldForConflicts, lbdTrackingOptions.CoolDownConflicts);

        ConstraintFactory = new ConstraintFactory(LearnedConstraints);
        PreProcessor = new PreProcessor<ConstraintFactory>(options, problem, UnitPropagationQueue, Variables, Literals, ConstraintFactory);
        CandidateHeap = new CandidateHeap<ConstraintFactory>(Variables, ConstraintFactory);
        VariableTrail = new VariableTrail<CandidateHeap<ConstraintFactory>>(CandidateHeap, Variables.Length);
        ActivityManager = new ActivityManager<CandidateHeap<ConstraintFactory>>(Variables, LearnedConstraints, CandidateHeap, options);
        VariablePropagator = new VariablePropagator<VariableTrail<CandidateHeap<ConstraintFactory>>, ActivityManager<CandidateHeap<ConstraintFactory>>, PropagationRateTracker>(VariableTrail, UnitPropagationQueue, ActivityManager, PropagationRateTracker);
        LearnedConstraintCreator = new LearnedConstraintCreator<VariableTrail<CandidateHeap<ConstraintFactory>>, ActivityManager<CandidateHeap<ConstraintFactory>>>(VariableTrail, ActivityManager);
        LearnedConstraintsReducer = new LearnedConstraintsReducer<ConstraintFactory>(options, LearnedConstraints, ConstraintFactory);
        RestartManager = new RestartManager<
            VariableTrail<CandidateHeap<ConstraintFactory>>,
            PropagationRateTracker,
            LiteralBlockDistanceTracker,
            LearnedConstraintsReducer<ConstraintFactory>,
            LubySequence>(
            options,
            VariableTrail,
            PropagationRateTracker,
            LiteralBlockDistanceTracker,
            UnitPropagationQueue,
            LearnedConstraintsReducer,
            options.Restart.Interval is not null && options.Restart.Luby ? new LubySequence(options.Restart.Interval.Value) : null);

        ConstraintMinimizer = new ConstraintMinimizer();

        ConflictHandler = new ConflictHandler<
            ActivityManager<CandidateHeap<ConstraintFactory>>,
            VariableTrail<CandidateHeap<ConstraintFactory>>,
            PropagationRateTracker,
            LiteralBlockDistanceTracker,
            LearnedConstraintCreator<VariableTrail<CandidateHeap<ConstraintFactory>>, ActivityManager<CandidateHeap<ConstraintFactory>>>,
            RestartManager<VariableTrail<CandidateHeap<ConstraintFactory>>, PropagationRateTracker, LiteralBlockDistanceTracker, LearnedConstraintsReducer<ConstraintFactory>, LubySequence>,
            ConstraintMinimizer,
            ConstraintFactory>
            (
            options,
            Literals,
            ActivityManager,
            VariableTrail,
            PropagationRateTracker,
            LiteralBlockDistanceTracker,
            LearnedConstraintCreator,
            UnitPropagationQueue,
            RestartManager,
            ConstraintMinimizer,
            ConstraintFactory);
    }
}