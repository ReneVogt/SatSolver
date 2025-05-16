using Revo.SatSolver.DataStructures;
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
    const double _rescaleLimit = 1e100;

    readonly CancellationToken _cancellationToken;
    readonly Options _options;
    
    readonly Variable[] _variables;
    readonly Queue<(ConstraintLiteral, Constraint Reason)> _unitLiterals = [];
    readonly Stack<(int variableTrailIndex, bool first)> _decisionLevels = [];
    readonly Variable[] _variableTrail;

    readonly List<Constraint> _learnedConstraints = [];

    readonly CandidateHeap _candidateHeap;

    readonly LubySequence? _lubySequence;

    readonly int _originalClauseCount;

    readonly EmaTracker? _literalBlockDistanceTracker;
    readonly PropagationRateTracker? _propagationRateTracker;

    int _variableTrailSize;
    int _restartCounter, _nextRestartThreshold;
    bool _restartRecommended;

    double _clauseActivityIncrement = 1, _variableActivityIncrement = 1;

    SatSolver(Problem problem, Options options, CancellationToken cancellationToken)
    {
        if (options.VariableActivityDecayFactor == 0 || options.ClauseActivityDecayFactor == 0) throw new ArgumentException(paramName: nameof(options), message: "A decay factor must not be zero.");

        _options = options;
        _cancellationToken = cancellationToken;
        _variables = [..Enumerable.Range(0, problem.NumberOfLiterals).Select(index => new Variable(index))];
        _variableTrail = new Variable[problem.NumberOfLiterals];

        if (_options.LiteralBlockDistanceTracking is { Decay: var lbdDecay, RecentCount: var lbdSize } && 
            (_options.Restart is { LiteralBlockDistanceThreshold: not null} || 
            !_options.OnlyPoorMansVSIDS && _options.ClauseDeletion is { LiteralBlockDistanceThreshold: not null}))
            _literalBlockDistanceTracker = new(lbdSize, lbdDecay);
        
        _originalClauseCount = BuildConstraints(problem.Clauses);

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

        _candidateHeap = new(Enumerable.Range(0, problem.NumberOfLiterals).Select(i => _variables[i].Activity));
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
        for (var i = 0; i<variables.Length; i++)        
            variables[i].Activity /= maxActivity;

        _variableActivityIncrement /= _options.VariableActivityDecayFactor;

        return clauseCount;
    }

    Literal[]? Solve()
    {
        if (!PropagateUnits())
            return null;

        _variableTrailSize = 0;

        Variable? candidateVariable = null;
        var candidateSense = true;

        for(; ; )
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var firstTry = false;
            if (candidateVariable is null)
            {
                (candidateVariable, candidateSense) = GetNextCandidate();
                if (candidateVariable is null) return BuildSolution();
                firstTry = true;
            }

            _decisionLevels.Push((_variableTrailSize, first: firstTry));
            var success = PropagateVariable(candidateVariable, candidateSense, null) && PropagateUnits();

            candidateVariable = null;

            if (_restartRecommended)
            {
                Restart();
                continue;
            }

            if (!success)
            {
                (candidateVariable, candidateSense) = Backtrack();
                if (candidateVariable is null) return null;
            }
        }
    }
    (Variable? Variable, bool Sense) GetNextCandidate()
    {
        var variables = _variables;
        while(_candidateHeap.Count > 0)
        {
            var variableIndex = _candidateHeap.Dequeue();
            var variable = variables[variableIndex];
            if (variable.Sense is null) return (variable, variable.Polarity);
        }

        return (null, false);
    }

    void ResetVariableTrail(int targetLevelStart)
    {
        foreach (var trailedVariable in _variableTrail[targetLevelStart.._variableTrailSize])
        {
            trailedVariable.Sense = null;
            trailedVariable.Reason = null;
            trailedVariable.DecisionLevel = 0;

            _candidateHeap.Enqueue(trailedVariable.Index, trailedVariable.Activity);
        }
        
        _variableTrailSize = targetLevelStart;
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
        _decisionLevels.Clear();
        ResetVariableTrail(0);
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
