using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;

namespace Revo.SatSolver.Processors;

sealed class SatSolverInitializer : IInitializeSatSolver
{
    readonly Problem _problem;
    readonly SatSolverOptions _options;
    readonly CancellationToken _cancellationToken;

    public SatSolverInitializer(Problem problem, SatSolverOptions options, CancellationToken cancellationToken)
    {
        if (options.VariableActivityDecayFactor == 0 || 
            options.ConstraintActivityDecayFactor == 0) 
            throw new ArgumentException(paramName: nameof(options), message: "A decay factor must not be zero.");
        
        _problem = problem;
        _options = options;
        _cancellationToken = cancellationToken;
    }

    public ComponentStore Initialize()
    {
        var variables = Enumerable.Range(0, _problem.NumberOfLiterals).Select(index => new Variable(index)).ToArray();
        var literalBlockDistanceTracker = new LiteralBlockDistanceTracker(_options.LiteralBlockDistanceTracking.RecentCount, _options.LiteralBlockDistanceTracking.Decay);
        var propagationRateTracker = new PropagationRateTracker(_options.PropagationRateTracking.ConflictInterval, _options.PropagationRateTracking.SampleSize, _options.PropagationRateTracking.Decay);

        var unitPropagationQueue = new UnitPropagationQueue();

        // it is very important to do this before we 
        // initialize the heap with the variables and
        // activities!
        var originalClauseCount = BuildConstraints(_problem.Clauses, variables, unitPropagationQueue, _options);

        var candidateHeap = new CandidateHeap(variables);
        var trail = new VariableTrail<CandidateHeap>(candidateHeap, variables.Length);
        var learnedConstraints = new List<Constraint>();
        var activityManager = new ActivityManager<CandidateHeap>(variables, learnedConstraints, candidateHeap, _options);
        var constraintMinimizer = new ConstraintMinimizer();
        var learnedConstraintCreator = new LearnedConstraintCreator<VariableTrail<CandidateHeap>, ActivityManager<CandidateHeap>>(trail, activityManager);
        var constraintReducer = new LearnedConstraintsReducer<PropagationRateTracker, LiteralBlockDistanceTracker>(_options, learnedConstraints, originalClauseCount, propagationRateTracker, literalBlockDistanceTracker);

        var restartManager = new RestartManager<
            VariableTrail<CandidateHeap>,
            PropagationRateTracker,
            LiteralBlockDistanceTracker,
            LearnedConstraintsReducer<PropagationRateTracker, LiteralBlockDistanceTracker>,
            LubySequence>(
            trail,
            propagationRateTracker,
            literalBlockDistanceTracker,
            unitPropagationQueue,
            constraintReducer,
            _options.Restart.Interval,
            _options.Restart.Luby && _options.Restart.Interval is { } restartInterval ? new LubySequence(restartInterval) : null,
            _options.Restart.PropagationRateThreshold,
            _options.Restart.LiteralBlockDistanceThreshold,
            _options.ConstraintDeletion.ReduceOnRestart);        

        return new ComponentStore(
            _options,
            originalClauseCount,
            variables,
            unitPropagationQueue,
            candidateHeap,
            trail,
            new VariablePropagator<VariableTrail<CandidateHeap>, ActivityManager<CandidateHeap>, PropagationRateTracker>(trail, unitPropagationQueue, activityManager, propagationRateTracker),
            new ConflictHandler<
                ActivityManager<CandidateHeap>, 
                VariableTrail<CandidateHeap>,
                PropagationRateTracker, 
                LiteralBlockDistanceTracker,
                LearnedConstraintCreator<VariableTrail<CandidateHeap>, ActivityManager<CandidateHeap>>,
                RestartManager<VariableTrail<CandidateHeap>, PropagationRateTracker, LiteralBlockDistanceTracker, LearnedConstraintsReducer<PropagationRateTracker, LiteralBlockDistanceTracker>, LubySequence>,
                ConstraintMinimizer>
                (_options, variables, activityManager, trail, propagationRateTracker, literalBlockDistanceTracker, learnedConstraintCreator, learnedConstraints, unitPropagationQueue, restartManager, constraintMinimizer),
            learnedConstraintCreator,
            constraintReducer,
            constraintMinimizer,
            learnedConstraints,
            activityManager,
            literalBlockDistanceTracker,
            propagationRateTracker,
            restartManager,
            _cancellationToken);
    }
    static int BuildConstraints(IEnumerable<Clause> clauses, Variable[] variables, UnitPropagationQueue unitPropagationQueue, SatSolverOptions options)
    {
        var clauseCount = 0;
        var scores = new double[variables.Length << 1];
        var literals = new HashSet<ConstraintLiteral>();
        var tautologyTest = new StampArray();

        foreach (var clause in clauses)
        {
            literals.Clear();

            //
            // Map the literals in the clause to ConstraintLiterals
            // in the Constraint to generate.
            //
            foreach (var literal in clause.Literals)
                literals.Add(literal.Sense ? variables[literal.Id-1].PositiveLiteral : variables[literal.Id-1].NegativeLiteral);

            //
            // Check if the clause is a tautology (e.g. "a | !a")
            // to skip those immediatly fulfilled clauses.
            //
            tautologyTest.Clear();
            if (literals.Any(l => !tautologyTest.Add(l.Variable.Index))) continue;

            clauseCount++;

            //
            // Constraints with a single literal are
            // immediate unit propagations.
            //
            var constraint = new Constraint(literals);
            if (constraint.Literals.Length == 1)
                unitPropagationQueue.Enqueue((constraint.Watched1, constraint));

            //
            // Calculat Jeroslow-Wang heuristic for 
            // all litrals in the constraint.
            //
            foreach (var literal in literals)
            {
                var index = literal.Variable.Index << 1;
                if (!literal.Orientation) index+=1;
                scores[index] += Math.Pow(2, -constraint.Literals.Length);
            }
        }

        //
        // The sum of the two literals' Jeroslow-Wang score
        // is our initial activity for the variable. 
        //
        // The literal with the higher score determines the
        // polarity of the variable (note: the goal is to
        // fulfill as many constraints as possible, not to
        // generate many unit propagations).
        //
        var maxActivity = double.MinValue;
        for (var i = 0; i<variables.Length; i++)
        {
            var ps = scores[i<<1];
            var ns = scores[(i<<1)+1];
            var activity = ps + ns;
            if (activity > maxActivity) maxActivity = activity;
            variables[i].Activity = activity;
            variables[i].Polarity = ps > ns;
        }
        maxActivity /= options.VariableActivityDecayFactor;
        for (var i = 0; i<variables.Length; i++)
            variables[i].Activity /= maxActivity;

        return clauseCount;
    }
}
