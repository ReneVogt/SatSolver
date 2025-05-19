using Revo.SatSolver.DataStructures;
using Revo.SatSolver.DPLL;
using Revo.SatSolver.Helpers;

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

    readonly List<Constraint> _learnedConstraints = [];

    readonly CandidateHeap _candidateHeap;
    readonly ActivityManager _activityManager;

    readonly LubySequence? _lubySequence;

    readonly int _originalClauseCount;

    readonly EmaTracker? _literalBlockDistanceTracker;
    readonly PropagationRateTracker? _propagationRateTracker;

    int _restartCounter, _nextRestartThreshold;
    bool _restartRecommended;

    SatSolver(Problem problem, Options options, CancellationToken cancellationToken)
    {
        if (options.VariableActivityDecayFactor == 0 || options.ClauseActivityDecayFactor == 0) throw new ArgumentException(paramName: nameof(options), message: "A decay factor must not be zero.");

        _options = options;
        _cancellationToken = cancellationToken;
        _variables = [..Enumerable.Range(0, problem.NumberOfLiterals).Select(index => new Variable(index))];

        if (_options.LiteralBlockDistanceTracking is { Decay: var lbdDecay, RecentCount: var lbdSize } && 
            (_options.Restart is { LiteralBlockDistanceThreshold: not null} || 
            !_options.OnlyPoorMansVSIDS && _options.ClauseDeletion is { LiteralBlockDistanceThreshold: not null}))
            _literalBlockDistanceTracker = new(lbdSize, lbdDecay);
        
        if (_options.Restart?.Interval is { } restartInterval)
        {
            if (_options.Restart.Luby)
            {
                _lubySequence = new(restartInterval);
                _nextRestartThreshold = (int)_lubySequence.Next();
            }
            else
                _nextRestartThreshold = restartInterval;
        }

        if (_options.PropagationRateTracking is { ConflictInterval: var interval, Decay: var decay, SampleSize: var sampleSize } && 
            interval > 0  && sampleSize > 0 &&
            (_options.Restart is { PropagationRateThreshold: not null } ||
            !_options.OnlyPoorMansVSIDS && _options.ClauseDeletion is { PropagationRateThreshold: not null }))
            _propagationRateTracker = new(interval, sampleSize, decay);

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

            var backtrack = false;
            var decision = true;
            _trail.Push(firstTry);
            var conflictingConstraint = _dpllProcessor.PropagateVariable(candidateVariable, candidateSense, null, out propagationCount);
            if (conflictingConstraint is null)
            {
                decision = false;
                conflictingConstraint = _dpllProcessor.PropagateUnits(ref propagationCount);
            }

            TrackPropagationRate(propagationCount);

            if (conflictingConstraint is not null)
            {
                TrackPropagationRate(null);
                backtrack = HandleConflict(conflictingConstraint, decision);
            }

            candidateVariable = null;

            if (_restartRecommended)
            {
                Restart();
                continue;
            }

            if (backtrack)
            {
                _unitLiterals.Clear();
                (candidateVariable, candidateSense) = _trail.Backtrack();
                if (candidateVariable is null) return null;
            }
        }
    }
    bool HandleConflict(Constraint conflictingConstraint, bool decision)
    {
        if (_nextRestartThreshold > 0)
        {
            _restartCounter++;
            _restartRecommended = _restartCounter > _nextRestartThreshold;
        }

        if (_options.OnlyPoorMansVSIDS)
        {
            _activityManager.IncreaseVariableActivity(conflictingConstraint);
            return true;
        }

        _activityManager.IncreaseConstraintActivity(conflictingConstraint);

        if (decision || _trail.DecisionLevel == 0) return true;
        PerformClauseLearning(conflictingConstraint);
        return false;
    }
    void TrackPropagationRate(int? propagations)
    {
        if (_propagationRateTracker is null) return;
        if (propagations is null)
            _propagationRateTracker.AddConflict();
        else
            _propagationRateTracker.AddPropagations(propagations.Value);

        if (_options.Restart is { PropagationRateThreshold: { } restartThreshold } &&  _propagationRateTracker.CurrentRatio < restartThreshold)
            _restartRecommended = true;
        if (_options.ClauseDeletion is { PropagationRateThreshold: { } clauseDeletionThreshold } && _propagationRateTracker.CurrentRatio < clauseDeletionThreshold)
            ReduceClauses();
    }
    void Restart()
    {
        _restartRecommended = false;
        _restartCounter = 0;
        if (_lubySequence is not null)
        {
            var next = _lubySequence.Next();
            _nextRestartThreshold = next < int.MaxValue ? (int)next : 0;
        }
        _trail.Reset();
        _unitLiterals.Clear();
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
