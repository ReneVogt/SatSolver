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
    
    readonly Variable[] _literals;
    readonly Queue<(int Literal, Constraint Reason)> _unitLiterals = [];
    readonly Stack<(int variableTrailIndex, bool first)> _decisionLevels = [];
    readonly int[] _variableTrail;

    readonly HashSet<Constraint> _learnedConstraints = [];

    readonly LubySequence? _lubySequence;

    readonly int _originalClauseCount;

    readonly EmaTracker? _literalBlockDistanceTracker;

    int _variableTrailSize;
    int _restartCounter, _nextRestartThreshold;
    bool _restartRecommended;

    double _clauseActivityIncrement = 1, _variableActivityIncrement = 1;

    SatSolver(Problem problem, Options options, CancellationToken cancellationToken)
    {
        if (options.VariableActivityDecayFactor == 0 || options.ClauseActivityDecayFactor == 0) throw new ArgumentException(paramName: nameof(options), message: "A decay factor must not be zero.");

        _options = options;
        _cancellationToken = cancellationToken;
        _literals = new Variable[problem.NumberOfLiterals << 1];
        _variableTrail = new int[problem.NumberOfLiterals];

        if (_options.LiteralBlockDistanceTracking is { Decay: var lbdDecay, RecentCount: var lbdSize } && 
            (_options.RestartOptions is { LiteralBlockDistanceThreshold: not null} || 
            _options.ClauseDeletionOptions is { LiteralBlockDistanceThreshold: not null}))
            _literalBlockDistanceTracker = new(lbdSize, lbdDecay);
        
        _originalClauseCount = BuildConstraints(problem.Clauses);

        if (_options.RestartOptions?.Interval is { } restartInterval)
        {
            if (_options.RestartOptions.Luby)
            {
                _lubySequence = new(restartInterval);
                _nextRestartThreshold = (int)_lubySequence.Next();
            }
            else
                _nextRestartThreshold = restartInterval;
        }
    }
    int BuildConstraints(IEnumerable<Clause> clauses)
    {
        var clauseCount = 0;
        var scores = new double[_literals.Length];
        var positives = new HashSet<int>();
        var negatives = new HashSet<int>();
        foreach (var clause in clauses)
        {
            positives.Clear();
            negatives.Clear();

            foreach (var literal in clause.Literals)
                if (literal.Sense)
                    positives.Add(literal.Id-1);
                else
                    negatives.Add(literal.Id-1);

            // test for tautology (a | !a)
            if (positives.Intersect(negatives).Any()) continue;

            clauseCount++;

            var literals = positives.Select(i => i << 1).Concat(negatives.Select(i => (i << 1) + 1)).ToHashSet();
            var constraint = new Constraint(literals);

            if (literals.Count == 1)
            {
                constraint.Watched1 = constraint.Watched2 = literals.First();
                _literals[constraint.Watched1].Watchers.Add(constraint);
                _unitLiterals.Enqueue((constraint.Watched1, constraint));
            }
            else
            {
                constraint.Watched1 = literals.First();
                constraint.Watched2 = literals.Skip(1).First();
                _literals[constraint.Watched1].Watchers.Add(constraint);
                _literals[constraint.Watched2].Watchers.Add(constraint);
            }

            foreach (var literal in literals)
                scores[literal] += Math.Pow(2, -literals.Count);
        }

        var maxActivity = double.MinValue;
        for (var i = 0; i<_literals.Length; i+=2)
        {
            var activity = scores[i] + scores[i+1];
            if (activity > maxActivity) maxActivity = activity;
            _literals[i].Activity = activity;
            _literals[i].Polarity = scores[i] > scores[i+1];
        }
        for (var i = 0; i<_literals.Length; i+=2)        
            _literals[i].Activity /= maxActivity;

        _variableActivityIncrement /= _options.VariableActivityDecayFactor;

        return clauseCount;
    }

    Literal[]? Solve()
    {
        if (!PropagateUnits())
            return null;

        _variableTrailSize = 0;

        var candidateVariable = -1;
        var candidateSense = true;

        for(; ; )
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var firstTry = false;
            if (candidateVariable < 0)
            {
                (candidateVariable, candidateSense) = GetNextCandidate();
                if (candidateVariable < 0) return BuildSolution();
                firstTry = true;
            }

            _decisionLevels.Push((_variableTrailSize, first: firstTry));
            var success = PropagateVariable(candidateVariable, candidateSense, null) && PropagateUnits();

            candidateVariable = -1;

            if (_restartRecommended)
            {
                Restart();
                continue;
            }

            if (!success)
            {
                (candidateVariable, candidateSense) = Backtrack();
                if (candidateVariable == -1) return null;
            }
        }
    }
    (int Variable, bool Sense) GetNextCandidate()
    {
        var variable = -1;
        var sense = false;
        var best = double.MinValue;
        for (var i = 0; i < _literals.Length; i+=2)
        {
            var literal = _literals[i];
            if (literal.Sense is null && literal.Activity > best)
            {
                variable = i >> 1;
                sense = literal.Polarity;
                best = literal.Activity;
            }
        }

        return (variable, sense);
    }

    void ResetVariableTrail(int targetLevelStart)
    {
        foreach (var trailedVariable in _variableTrail[targetLevelStart.._variableTrailSize])
        {
            var positiveLiteral = trailedVariable << 1;
            _literals[positiveLiteral].Sense = null;
            _literals[positiveLiteral].Reason = null;
            _literals[positiveLiteral].DecisionLevel = 0;

            _literals[positiveLiteral+1].Sense = null;
        }
        
        _variableTrailSize = targetLevelStart;
    }
    void Restart()
    {
        _restartRecommended = false;
        _restartCounter = 0;
        _literalBlockDistanceTracker?.Reset();
        if (_lubySequence is not null)
        {
            var next = _lubySequence.Next();
            _nextRestartThreshold = next < int.MaxValue ? (int)next : 0;
        }
        _decisionLevels.Clear();
        ResetVariableTrail(0);
        _unitLiterals.Clear();
    }

    Literal[] BuildSolution() => [.. Enumerable.Range(0, _literals.Length >> 1).Select(i => new Literal(i+1, _literals[i << 1].Sense!.Value))];

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
