using Revo.SatSolver;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace SatSolverTests.Stubs;

sealed class TestState(
    SatSolverOptions options, 
    int originalConstraintCount, 
    Variable[] variables, 
    Queue<(ConstraintLiteral UnitLiteral, Constraint Reason)> unitsToPropagate, 
    Func<TestState, string,object> _stateGetter) : SatSolverState(options, originalConstraintCount, variables, unitsToPropagate, CancellationToken.None)
{
    public override ICandidateHeap CandidateHeap => (ICandidateHeap)_stateGetter(this, nameof(CandidateHeap));
    public override IVariableTrail VariableTrail => (IVariableTrail)_stateGetter(this, nameof(VariableTrail));
    public override IVariablePropagator VariablePropagator => (IVariablePropagator)_stateGetter(this, nameof(VariablePropagator));
    public override IConflictDrivenClauseLearner CdclProcessor => (IConflictDrivenClauseLearner)_stateGetter(this, nameof(CdclProcessor));
    public override IActivityManager ActivityManager => (IActivityManager)_stateGetter(this, nameof(ActivityManager));
    public override EmaTracker LiteralBlockDistanceTracker => (EmaTracker)_stateGetter(this, nameof(LiteralBlockDistanceTracker));
    public override PropagationRateTracker PropagationRateTracker => (PropagationRateTracker)_stateGetter(this, nameof(PropagationRateTracker));
    public override RestartManager RestartManager => (RestartManager)_stateGetter(this, nameof(RestartManager));
    public override ICreateLearnedConstraints LearnedConstraintCreator => (ICreateLearnedConstraints)_stateGetter(this, nameof(LearnedConstraintCreator));
    public override IReduceLearnedConstraints LearnedConstraintsReducer => (IReduceLearnedConstraints)_stateGetter(this, nameof(LearnedConstraintsReducer));
    public override IMinimizeConstraints ConstraintMinimizer => (IMinimizeConstraints)_stateGetter(this, nameof(ConstraintMinimizer));
}
