using Revo.SatSolver.Properties;

namespace Revo.SatSolver;

public sealed class SatSolver
{
    readonly CancellationToken _cancellationToken;
    readonly Variable[] _variables;
    readonly HashSet<int> _fixedVariables = [];
    readonly HashSet<int> _freeVariables;
    readonly Constraint[] _clauses;
    readonly HashSet<int> _activeClauses;
    readonly Stack<(Variable variable, bool guessed)> _stack = [];

    /// <summary>
    /// This constructor initializes the data structures for the DPLL
    /// algorithm. It creates variables and constraints (the inner
    /// representation of clauses) and the connections between them.
    /// It also removes clauses that are obviously satisfied (e.g
    /// contain 'a -a').
    /// </summary>
    /// <param name="problem">The <see cref="Problem"/> definition to solve.</param>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> was <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The problem contains either invalid literal IDs or no literals at all.</exception>
    SatSolver(Problem problem, CancellationToken cancellationToken)
    { 
        _ = problem ?? throw new ArgumentNullException(nameof(problem));
        _cancellationToken = cancellationToken;

        if (problem.NumberOfLiterals < 1)
            throw new ArgumentException(Resources.SatSolverArgumentException_NumberOfLiterals, nameof(problem));
        if (problem.Clauses.SelectMany(clause => clause.Literals.Select(literal => Math.Abs(literal.Id))).Any(id => id < 1 || id > problem.NumberOfLiterals))
            throw new ArgumentException(Resources.SatSolverArgumentException_InvalidLiterals, nameof(problem));

        // Literals are 1-indexed, so we add an ignored variable at index 0 that will
        // not appear in the _freeVariables set.
        _variables = Enumerable.Range(0, problem.NumberOfLiterals+1).Select(i => new Variable(i)).ToArray();
        _freeVariables = new(Enumerable.Range(1, problem.NumberOfLiterals));

        _clauses = problem.Clauses.Select(ConstraintGenerator).ToArray();
        _activeClauses = new(Enumerable.Range(0, _clauses.Length).Where(IsNotObviouslyTrue));

        Constraint ConstraintGenerator(Clause clause, int id)
        {
            var constraint = new Constraint();
            foreach (var literal in clause.Literals)
                if (literal.Sense)
                {
                    constraint.Positives.Add(literal.Id);
                    _variables[literal.Id].Positives.Add(id);
                }
                else
                {
                    constraint.Negatives.Add(literal.Id);
                    _variables[literal.Id].Negatives.Add(id);
                }
            return constraint;
        }
        bool IsNotObviouslyTrue(int i)
        {
            var clause = _clauses[i];
            return !clause.Positives.Intersect(clause.Negatives).Any();
        }

    }

    /// <summary>
    /// Unit propgation: we look for clauses that contain only a single
    /// literal. This literal can be fixed according to its sense and
    /// all clauses where the literal occurs in the same sense can be
    /// removed and all occurences of this literal in a different sense
    /// can be removed from the respective clauses.
    /// </summary>
    /// <returns><c>true</c> if changes were made, <c>false</c> if not.</returns>
    void UnitPropagation()
    {
        while(_activeClauses.Select(i => _clauses[i]).FirstOrDefault(clause => clause.Positives.Count + clause.Negatives.Count == 1) is { } clause)
        {
            var variable = _variables[clause.Positives.Count == 0 ? clause.Negatives.First() : clause.Positives.First()];
            Propagate(variable, sense: clause.Positives.Count == 1, guessed: _stack.Count == 0);
        }
    }

    /// <summary>
    /// Pure Literal Elimination: if a literal only occures in one sense, we can 
    /// safely set it to its sense to satisfy the clauses it's contained in.
    /// </summary>
    /// <returns><c>true</c> if clauses were changed, <c>false</c> if not (or clauses were removed).</returns>
    void PureLiteralElimination()
    {
        while (_freeVariables.Select(id => _variables[id]).FirstOrDefault(variable => variable.Positives.Count == 0 || variable.Negatives.Count == 0) is { } variable)
            Propagate(variable, sense: variable.Negatives.Count == 0, guessed: false);
    }


    /// <summary>
    /// Propagates a variable change. Sets the variable from
    /// the free set to the fixed set, removes all clauses
    /// that are now fulfilled and sets their <see cref="Constraint.RemovalIndex"/>
    /// to the current stack index to know when to reactive
    /// them.
    /// The variable is removed from all clauses where it 
    /// evaluates to false.
    /// </summary>
    /// <param name="variable">The variable to fix.</param>
    /// <param name="sense">The sense to set the variable to.</param>
    /// <param name="guessed">Indicates wether this is a guess or an optimization.</param>
    void Propagate(Variable variable, bool sense, bool guessed)
    {
        _freeVariables.Remove(variable.Id);
        _fixedVariables.Add(variable.Id);
        variable.Sense = sense;

        //
        // Remove fulfilled clauses.
        //
        foreach (var index in (variable.Sense ? variable.Positives : variable.Negatives).Intersect(_activeClauses))
        {
            _activeClauses.Remove(index);
            _clauses[index].RemovalIndex = _stack.Count;
        }

        //
        // Remove variable from clauses it cannot satisfy.
        //
        if (variable.Sense)
            foreach (var clause in variable.Negatives.Select(i => _clauses[i]))
                clause.Negatives.Remove(variable.Id);
        else
            foreach (var clause in variable.Positives.Select(i => _clauses[i]))
                clause.Positives.Remove(variable.Id);

        _stack.Push((variable, guessed));
    }

    /// <summary>
    /// The stack knows which variable was set and if that
    /// was a guess or an optimization.
    /// We can know restore the data to the situation before
    /// that change.
    /// We go back until the last guessed variable. If it was
    /// guessed false, we now guess it true, otherwise we try
    /// to go back further..
    /// If we reached the bottom of the stack without any more
    /// guessed variables, the search has finished.
    /// </summary>
    /// <returns><c>true</c> if we changed a variable and
    /// start over, <c>false</c> if not and we finished.</returns>
    bool Pop() 
    {
        while(_stack.Count > 0)
        {
            var (variable, guessed) = _stack.Pop();
            _fixedVariables.Remove(variable.Id);
            _freeVariables.Add(variable.Id);

            //
            // If the variable was set to false, 
            // we removed all clauses where it was
            // negative and removed it from all clauses
            // where it was positive.
            // And vice versa.
            //

            //
            // Re-add the variable to the clauses.
            //
            if (variable.Sense)
                foreach(var clause in variable.Negatives.Select(i => _clauses[i]))
                    clause.Negatives.Add(variable.Id);
            else
                foreach (var clause in variable.Positives.Select(i => _clauses[i]))
                    clause.Positives.Add(variable.Id);

            //
            // Re-activate the clauses that were remove
            // because of the variable.
            //
            foreach(var index in (variable.Sense ? variable.Positives : variable.Negatives).Where(index => _clauses[index].RemovalIndex == _stack.Count))
            {
                _clauses[index].RemovalIndex = 0;
                _activeClauses.Add(index);
            }

            //
            // if the variable was guessed false,
            // we switch, otherwise we keep going
            // down the stack.
            //
            if (guessed && !variable.Sense)
            {
                Propagate(variable, true, guessed);
                return true;
            }
        }

        return false;
    }

    Variable GetNextCandidate() => _freeVariables.Select(id => _variables[id]).MaxBy(v => v.Negatives.Count + v.Positives.Count)!;

    IEnumerable<Literal[]> Solve()
    {
        do
        {
            _cancellationToken.ThrowIfCancellationRequested();

            UnitPropagation();
            PureLiteralElimination();

            //
            // Check if all clauses are satisfied.
            //
            if (_activeClauses.Count == 0)
            {
                yield return _fixedVariables.Select(id => new Literal(id, _variables[id].Sense)).ToArray();
                if (!Pop()) yield break;
                continue;
            }

            //
            // Check if there is an empty clause, meaning
            // this clause could never be satisfied.
            //
            if (_activeClauses.Select(i => _clauses[i]).Any(clause => clause.Positives.Count + clause.Negatives.Count == 0))
            {
                if (!Pop()) yield break;
                continue;
            }

            //
            // Take the most frequent variable and guess it.
            //
            var variable = GetNextCandidate();
            Propagate(variable, sense: false, guessed: true);

        } while (true);
    }

    /// <summary>
    /// Tries to enumerate solutions that satisfy the given <paramref name="problem"/>.
    /// Multiple solutions will only be produced when the returned sequence is enumerated.
    /// </summary>
    /// <param name="problem">The <see cref="Problem"/> to satisfy.</param>
    /// <returns>A sequence of solutions. The sequence is empty if no solution was found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> was <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The problem contains either invalid literal IDs or no literals at all.</exception>
    public static IEnumerable<Literal[]> Solve(Problem problem, CancellationToken cancellationToken = default)
    {
        var solver = new SatSolver(problem, cancellationToken);
        return solver.Solve();
    }
}
