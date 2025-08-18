using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;

namespace Revo.SatSolver;

sealed class SatSolverInitializer(Problem _problem, SatSolverOptions _options, CancellationToken _cancellationToken) : IInitializeSatSolver
{
    public SatSolverState Initialize()
    {
        if (_options.VariableActivityDecayFactor == 0 || _options.ConstraintActivityDecayFactor == 0) throw new ArgumentException(paramName: nameof(_options), message: "A decay factor must not be zero.");

        var variables = Enumerable.Range(0, _problem.NumberOfLiterals).Select(index => new Variable(index)).ToArray();
        var literalBlockDistanceTracker = new EmaTracker(_options.LiteralBlockDistanceTracking.RecentCount, _options.LiteralBlockDistanceTracking.Decay);
        var propagationRateTracker = new PropagationRateTracker(_options.PropagationRateTracking.ConflictInterval, _options.PropagationRateTracking.SampleSize, _options.PropagationRateTracking.Decay);

        var unitsToPropagate = new Queue<(ConstraintLiteral UnitLiteral, Constraint Reason)>();
        var learnedConstraints = new List<Constraint>();

        // it is very important to do this before we 
        // initialize the heap with the variables and
        // activities!
        var originalClauseCount = BuildConstraints(_problem.Clauses, variables, unitsToPropagate, _options);
        return new SatSolverStateInternal(_options, originalClauseCount, variables, unitsToPropagate, _cancellationToken);
    }
    static int BuildConstraints(IEnumerable<Clause> clauses, Variable[] variables, Queue<(ConstraintLiteral UnitLiteral, Constraint Reason)> unitsToPropagate, SatSolverOptions options)
    {
        var clauseCount = 0;
        var scores = new double[variables.Length << 1];
        var literals = new HashSet<ConstraintLiteral>();
        var tautologyTest = new HashSet<int>();

        foreach (var clause in clauses)
        {
            literals.Clear();

            foreach (var literal in clause.Literals)
                literals.Add(literal.Sense ? variables[literal.Id-1].PositiveLiteral : variables[literal.Id-1].NegativeLiteral);

            // test for tautology (a | !a)
            tautologyTest.Clear();
            if (literals.Any(l => !tautologyTest.Add(l.Variable.Index))) continue;

            clauseCount++;

            var constraint = new Constraint(literals);
            if (constraint.Literals.Length == 1)
                unitsToPropagate.Enqueue((constraint.Watched1, constraint));

            foreach (var literal in literals)
            {
                var index = literal.Variable.Index << 1;
                if (!literal.Orientation) index+=1;
                scores[index] += Math.Pow(2, -constraint.Literals.Length);
            }
        }

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
