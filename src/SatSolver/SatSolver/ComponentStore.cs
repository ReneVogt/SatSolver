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

    public ComponentStore(SatSolverOptions options, Problem problem) : base(options, problem.NumberOfLiterals)
    {
        var propagationTrackingOptions = options.PropagationRateTracking;
        PropagationRateTracker = new PropagationRateTracker(propagationTrackingOptions.LocalHalflife, propagationTrackingOptions.GlobalHalflife, propagationTrackingOptions.Threshold, propagationTrackingOptions.HoldForConflicts, propagationTrackingOptions.CoolDownConflicts);
        var lbdTrackingOptions = options.LiteralBlockDistanceTracking;
        LiteralBlockDistanceTracker = new LiteralBlockDistanceTracker(lbdTrackingOptions.LocalHalflife, lbdTrackingOptions.GlobalHalflife, lbdTrackingOptions.Threshold, lbdTrackingOptions.HoldForConflicts, lbdTrackingOptions.CoolDownConflicts);
        var statistics = new Statistics(PropagationRateTracker, LiteralBlockDistanceTracker);

        ConstraintFactory = new ConstraintFactory(Literals, LearnedConstraints);
        PreProcessor = new PreProcessor(options, problem, UnitPropagationQueue, Variables, Literals, ConstraintFactory);
        CandidateHeap = new CandidateHeap(Variables, ConstraintFactory);
        VariableTrail = new VariableTrail(CandidateHeap, Variables.Length);
        ActivityManager = new ActivityManager(Variables, LearnedConstraints, CandidateHeap, options);
        VariablePropagator = new VariablePropagator(VariableTrail, UnitPropagationQueue, ActivityManager, PropagationRateTracker, statistics);
        LearnedConstraintCreator = new LearnedConstraintCreator(VariableTrail, ActivityManager);
        LearnedConstraintsReducer = new LearnedConstraintsReducer(options, LearnedConstraints, ConstraintFactory, statistics);
        RestartManager = new RestartManager(
            options,
            VariableTrail,
            PropagationRateTracker,
            LiteralBlockDistanceTracker,
            UnitPropagationQueue,
            LearnedConstraintsReducer,
            options.Restart.Interval is not null && options.Restart.Luby ? new LubySequence(options.Restart.Interval.Value) : null);

        ConstraintMinimizer = new ConstraintMinimizer();

        ConflictHandler = new ConflictHandler
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
            ConstraintFactory,
            statistics);
    }
}