using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace Revo.SatSolver;

abstract class SatSolverState
{
    public SatSolverOptions Options { get; }
    public int OriginalConstraintCount { get; }
    public Variable[] Variables { get; }
    public ConstraintLiteral[] Literals { get; }
    public Queue<(ConstraintLiteral UnitLiteral, Constraint Reason)> UnitsToPropagate { get; }
    public List<Constraint> LearnedConstraints { get; } = [];
    public CancellationToken CancellationToken { get; }

    public abstract ICandidateHeap CandidateHeap { get; }
    public abstract IVariableTrail VariableTrail { get; }
    public abstract IVariablePropagator VariablePropagator { get; }
    public abstract IConflictDrivenClauseLearner CdclProcessor { get; }

    public abstract IActivityManager ActivityManager { get; }
    public abstract EmaTracker LiteralBlockDistanceTracker { get; }
    public abstract PropagationRateTracker PropagationRateTracker { get; }

    public abstract RestartManager RestartManager { get; }

    public abstract ICreateLearnedConstraints LearnedConstraintCreator { get; }
    public abstract IReduceLearnedConstraints LearnedConstraintsReducer { get; }
    public abstract IMinimizeConstraints ConstraintMinimizer { get; }

    private protected SatSolverState(SatSolverOptions options, int originalConstraintCount, Variable[] variables, Queue<(ConstraintLiteral UnitLiteral, Constraint Reason)> unitsToPropagate, CancellationToken cancellationToken)
    {
        Options = options;
        OriginalConstraintCount = originalConstraintCount;
        Variables = variables;
        Literals =  new ConstraintLiteral[2*variables.Length];
        for (var i = 0; i<variables.Length; i++)
        {
            Literals[i*2] = variables[i].PositiveLiteral;
            Literals[i*2+1] = variables[i].NegativeLiteral;
        }
        UnitsToPropagate = unitsToPropagate;
        CancellationToken = cancellationToken;
    }
}