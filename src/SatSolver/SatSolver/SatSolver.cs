using System.Runtime.CompilerServices;

namespace Revo.SatSolver;

/// <summary>
/// Finds a variable configuration that 
/// satisfies all clauses in a SATisfiability 
/// problem.
/// </summary>
public sealed class SatSolver
{
    struct Variable
    {
        public bool? Sense { get; set; }
        public double Activity { get; set; }
        public bool Polarity { get; set; }

        List<Constraint>? _watchers;
        public List<Constraint> Watchers => _watchers ??= [];

        public override readonly string ToString() => $"Sense: {Sense?.ToString() ?? "null"} Activity: {Activity} Polarity: {Polarity}";
    }
    class Constraint(int[] _literals)
    {
        public int[] Literals => _literals;
        public int Watched1 { get; set; } = -1;
        public int Watched2 { get; set; } = -1;
    }

    const double DECAY_FACTOR = 0.97;
    const int DECAY_INTERVAL = 80;

    readonly CancellationToken _cancellationToken;
    
    readonly Variable[] _literals;

    readonly Queue<int> _unitLiterals = [];

    readonly Stack<(int variableTrailIndex, bool first)> _decisionLevels = [];
    readonly int[] _variableTrail;

    int _variableTrailSize, _conflictCount;

    SatSolver(Problem problem, CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _literals = new Variable[problem.NumberOfLiterals << 1];
        _variableTrail = new int[problem.NumberOfLiterals];
        BuildConstraints(problem.Clauses);
    }
    void BuildConstraints(IEnumerable<Clause> clauses)
    {
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

            var literals = positives.Select(i => i << 1).Concat(negatives.Select(i => (i << 1) + 1)).ToArray();
            var constraint = new Constraint(literals);

            if (literals.Length == 1)
            {
                _unitLiterals.Enqueue(literals[0]);
                constraint.Watched1 = constraint.Watched2 = literals[0];
                _literals[constraint.Watched1].Watchers.Add(constraint);
            }
            else
            {
                constraint.Watched1 = literals[0];
                constraint.Watched2 = literals[1];
                _literals[constraint.Watched1].Watchers.Add(constraint);
                _literals[constraint.Watched2].Watchers.Add(constraint);
            }
        }
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

            CheckVSIDSDecay();

            var firstTry = false;
            if (candidateVariable < 0)
            {
                (candidateVariable, candidateSense) = GetNextCandidate();
                if (candidateVariable < 0) return BuildSolution();
                firstTry = true;
            }

            _decisionLevels.Push((_variableTrailSize, first: firstTry));
            if (PropagateVariable(candidateVariable, candidateSense) && PropagateUnits())
            {
                candidateVariable = -1;
                continue;
            }

            (candidateVariable, candidateSense) = Backtrack();
            if (candidateVariable == -1) return null;
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
    bool PropagateVariable(int variable, bool sense) 
    {
        var positiveLiteralIndex = variable << 1;
        var negativeLiteralIndex = positiveLiteralIndex + 1;
        _literals[positiveLiteralIndex].Sense = sense;
        _literals[negativeLiteralIndex].Sense = !sense;
        _variableTrail[_variableTrailSize++] = variable;

        var watchedLiteral = sense ? negativeLiteralIndex : positiveLiteralIndex;
        var watchers = _literals[watchedLiteral].Watchers;
        for(var watcherIndex = 0; watcherIndex<watchers.Count; watcherIndex++)
        {
            var constraint = watchers[watcherIndex];
            if (constraint.Watched1 == watchedLiteral)
            {
                constraint.Watched1 = constraint.Watched2;
                constraint.Watched2 = watchedLiteral;
            }

            var otherWatchedSense = _literals[constraint.Watched1].Sense;
            if (otherWatchedSense == true) continue;

            var nextLiteral = -1;
            for (var i = 0; i<constraint.Literals.Length; i++)
            {
                var next = constraint.Literals[i];
                if (next == watchedLiteral || next == constraint.Watched1) continue;
                var nextSense = _literals[next].Sense;
                if (nextSense != false) nextLiteral = next;
                if (nextSense == true) break;
            }

            if (nextLiteral < 0)
            {
                if (otherWatchedSense is not null)
                {
                    _conflictCount++;
                    foreach (var literalIndex in constraint.Literals) _literals[literalIndex & -2].Activity += 1;
                    return false;
                }
                _unitLiterals.Enqueue(constraint.Watched1);
                continue;
            }
            
            constraint.Watched2 = nextLiteral;
            _literals[nextLiteral].Watchers.Add(constraint);
            watchers.RemoveAt(watcherIndex--);
        }

        _literals[positiveLiteralIndex].Polarity = sense;
        return true;
    }
    void ResetVariable(int variable)
    {
        var positiveLiteral = variable << 1;
        _literals[positiveLiteral].Sense = null;
        _literals[positiveLiteral+1].Sense = null;
    }
    bool PropagateUnits() 
    {
        while(_unitLiterals.Count > 0)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var literal = _unitLiterals.Dequeue();
            if (_literals[literal].Sense is not null) continue;
            if (!PropagateVariable(literal >> 1, (literal & 1) == 0)) return false;
        }
        return true;
    }
    (int Variable, bool Sense) Backtrack()
    {
        _unitLiterals.Clear();

        var first = false;
        var variableTrailIndex = -1;
        while (_decisionLevels.Count > 0 && !first) (variableTrailIndex, first) = _decisionLevels.Pop();
        if (!first) return (-1, true);

        var variable = _variableTrail[variableTrailIndex];
        var sense = !_literals[variable << 1].Sense!.Value;

        foreach (var trailedVariable in _variableTrail[variableTrailIndex.._variableTrailSize])
            ResetVariable(trailedVariable);

        _variableTrailSize = variableTrailIndex;
        return (variable, sense);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void CheckVSIDSDecay()
    {
        if (_conflictCount < DECAY_INTERVAL) return;
        _conflictCount = 0;
        for (var i = 0; i<_literals.Length; i+=2)
            _literals[i].Activity *= DECAY_FACTOR;
    }

    Literal[] BuildSolution() => [.. Enumerable.Range(0, _literals.Length >> 1).Select(i => new Literal(i+1, _literals[i << 1].Sense!.Value))];

    /// <summary>
    /// Finds a variable configuration that satisfies the SATisfiability <paramref name="problem"/>.
    /// If there is no solution the method return, <c>null</c>.
    /// </summary>
    /// <param name="problem">The <see cref="Problem"/> to satisfy.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>If a solution was found the method returns an array of <see cref="Literal"/>s indicating
    /// their senses that solve the problem. If no solution was found the method returns <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> was <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The problem contains either invalid literal IDs or no literals at all.</exception>
    public static Literal[]? Solve(Problem problem, CancellationToken cancellationToken = default)
    {
        _ = problem ?? throw new ArgumentNullException(nameof(problem));
        if (problem.Clauses.Any(clause => clause.Literals.Length == 0)) return null;
        if (problem.NumberOfLiterals == 0) return [];
        if (problem.Clauses.Length == 0) return [.. Enumerable.Range(1, problem.NumberOfLiterals).Select(i => new Literal(i, true))];

        var solver = new SatSolver(problem, cancellationToken);
        return solver.Solve();
    }
}
