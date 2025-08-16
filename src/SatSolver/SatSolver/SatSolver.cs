using Revo.SatSolver.CDCL;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
using Revo.SatSolver.Helpers;
using System.Diagnostics;

namespace Revo.SatSolver;

/// <summary>
/// Finds a variable configuration that 
/// satisfies all clauses in a SATisfiability 
/// problem.
/// </summary>
public sealed partial class SatSolver
{
    readonly CancellationToken _cancellationToken;
    readonly Options _options;
    
    readonly Variable[] _variables;
    readonly Queue<(ConstraintLiteral, Constraint Reason)> _unitLiterals = [];
    readonly VariableTrail _trail;
    readonly DpllProcessor _dpllProcessor;
    
    readonly LearnedConstraintsReducer _learnedConstraintsReducer;
    readonly CdclProcessor _cdclProcessor;

    readonly List<Constraint> _learnedConstraints = [];

    readonly CandidateHeap _candidateHeap;
    readonly ActivityManager _activityManager;

    readonly int _originalClauseCount;

    readonly EmaTracker _literalBlockDistanceTracker;
    readonly PropagationRateTracker _propagationRateTracker;

    readonly RestartManager _restartManager;

    SatSolver(Problem problem, Options options, CancellationToken cancellationToken)
    {
        if (options.VariableActivityDecayFactor == 0 || options.ClauseActivityDecayFactor == 0) throw new ArgumentException(paramName: nameof(options), message: "A decay factor must not be zero.");

        _options = options;
        _cancellationToken = cancellationToken;
        _variables = [..Enumerable.Range(0, problem.NumberOfLiterals).Select(index => new Variable(index))];

        _literalBlockDistanceTracker = new(_options.LiteralBlockDistanceTracking.RecentCount, _options.LiteralBlockDistanceTracking.Decay);
        _propagationRateTracker = new(_options.PropagationRateTracking.ConflictInterval, _options.PropagationRateTracking.SampleSize, _options.PropagationRateTracking.Decay);

        // it is very important to do this before we 
        // initialize the heap with the variables and
        // activities!
        _originalClauseCount = BuildConstraints(problem.Clauses);

        _candidateHeap = new (_variables);
        _activityManager = new ActivityManager(
            variables: _variables, 
            learnedConstraints: _learnedConstraints, 
            variableActivityDecay: _options.VariableActivityDecayFactor, 
            constraintActivityDecay: _options.ClauseActivityDecayFactor, 
            candidateHeap: _candidateHeap);
        _trail = new(problem.NumberOfLiterals, _candidateHeap);
        _dpllProcessor = new(_trail, _unitLiterals, _activityManager, _cancellationToken);

        _learnedConstraintsReducer = new(_options, _propagationRateTracker, _literalBlockDistanceTracker, _learnedConstraints, _originalClauseCount);
        var learnedConstraintCreator = new LearnedConstraintCreator(_trail, _activityManager, new ConstraintMinimizer(_options), _variables);
        _cdclProcessor = new(_options, _activityManager, _trail, _literalBlockDistanceTracker, learnedConstraintCreator, _learnedConstraints);

        _restartManager = new(_options, _trail, _propagationRateTracker, _literalBlockDistanceTracker, _unitLiterals);
    }
    int BuildConstraints(IEnumerable<Clause> clauses)
    {
        var clauseCount = 0;
        var scores = new double[_variables.Length << 1];
        var literals = new HashSet<ConstraintLiteral>();
        var tautologyTest = new HashSet<int>();
        var variables = _variables;
        
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
                _unitLiterals.Enqueue((constraint.Watched1, constraint));

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
        maxActivity /= _options.VariableActivityDecayFactor;
        for (var i = 0; i<variables.Length; i++)        
            variables[i].Activity /= maxActivity;

        return clauseCount;
    }

    Literal[]? Solve()
    {
        var propagationCount = 0;
        if (_dpllProcessor.PropagateUnits(ref propagationCount) is not null)
            return null;
        _trail.Clear();
        return _options.OnlyPoorMansVSIDS ? SolvePoor() : SolveCDCL();
    }

    Literal[]? SolvePoor()
    {
        Variable? candidateVariable = null;
        var candidateSense = true;

        for(; ; )
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var firstTry = false;

            if (candidateVariable is null)
            {
                candidateVariable = _candidateHeap.Dequeue();
                if (candidateVariable is null) return BuildSolution();
                candidateSense = candidateVariable.Polarity;
                firstTry = true;
            }

            _trail.Push(firstTry);
            Debug.WriteLine($"[{_trail.DecisionLevel}] Decided {candidateVariable.Index+1} to {candidateSense}.");
            var conflictingConstraint = _dpllProcessor.PropagateVariable(candidateVariable, candidateSense, null, out var propagationCount);
            conflictingConstraint ??= _dpllProcessor.PropagateUnits(ref propagationCount);

            _propagationRateTracker.AddPropagations(propagationCount);

            candidateVariable = null;
            if (conflictingConstraint is null) continue;

            Debug.WriteLine($"Conflict in {conflictingConstraint}");
            _propagationRateTracker.AddConflict();
            _restartManager.AddConflict();
            _activityManager.IncreaseVariableActivity(conflictingConstraint);

            if (_restartManager.RestartIfNecessary()) continue;
            
            Debug.WriteLine("Backtracking.");
            _unitLiterals.Clear();
            (candidateVariable, candidateSense) = _trail.Backtrack();
            if (candidateVariable is null) return null;
        }
    }
    Literal[]? SolveCDCL()
    {
        Variable? candidateVariable = null;
        Constraint? learnedConstraint = null;
        var candidateSense = true;

        for (;;)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (candidateVariable is null)
            {
                candidateVariable = _candidateHeap.Dequeue();
                if (candidateVariable is null) return BuildSolution();
                candidateSense = candidateVariable.Polarity;
                _trail.Push();
                Debug.WriteLine($"[{_trail.DecisionLevel}] Decided {candidateVariable.Index+1} to {candidateSense}.");
            }

            var conflictingConstraint = 
                _dpllProcessor.PropagateVariable(candidateVariable, candidateSense, learnedConstraint, out var propagationCount) ??
                _dpllProcessor.PropagateUnits(ref propagationCount);

            _propagationRateTracker.AddPropagations(propagationCount);

            candidateVariable = null;
            learnedConstraint = null;
            if (conflictingConstraint is null) continue;
            Debug.WriteLine($"Conflict in {conflictingConstraint} (learned: {conflictingConstraint.IsLearned}).");
            if (_trail.DecisionLevel == 0) return null;

            _propagationRateTracker.AddConflict();
            _restartManager.AddConflict();
            _activityManager.IncreaseConstraintActivity(conflictingConstraint);
            _unitLiterals.Clear();

            var (candidateLiteral, candidateReason) = _cdclProcessor.PerformClauseLearning(conflictingConstraint);
            _learnedConstraintsReducer.ReduceLearnedConstraintsIfNecessary();
            if (_restartManager.RestartIfNecessary()) continue;

            candidateVariable = candidateLiteral.Variable;
            candidateSense = candidateLiteral.Orientation;
            learnedConstraint = candidateReason;
            Debug.WriteLine($"[{_trail.DecisionLevel}] Propagating uip {candidateVariable.Index+1} to {candidateSense}.");
        }
    }

    Literal[] BuildSolution() => [.. _variables.Select(v => new Literal(v.Index+1, v.Sense!.Value))];

    /// <summary>
    /// Finds a variable configuration that satisfies the SATisfiability <paramref name="problem"/>.
    /// If there is no solution the method return, <c>null</c>.
    /// </summary>
    /// <param name="problem">The <see cref="Problem"/> to satisfy.</param>
    /// <param name="options">The options for the solver.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>If a solution was found the method returns an array of <see cref="Literal"/>s indicating
    /// their senses that solve the problem. If no solution was found the method returns <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> was <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The problem contains either invalid literal IDs or no literals at all.</exception>
    public static Literal[]? Solve(Problem problem, Options? options = null, CancellationToken cancellationToken = default)
    {
        _ = problem ?? throw new ArgumentNullException(nameof(problem));
        if (problem.Clauses.Any(clause => clause.Literals.Length == 0)) return null;
        if (problem.NumberOfLiterals == 0) return [];
        if (problem.Clauses.Length == 0) return [.. Enumerable.Range(1, problem.NumberOfLiterals).Select(i => new Literal(i, true))];

        var solver = new SatSolver(problem, options ?? Options.Default, cancellationToken);
        return solver.Solve();        
    }
}
