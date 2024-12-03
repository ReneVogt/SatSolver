namespace Revo.SatSolver.DPLL;
sealed class DpllProcessor
{ 
    enum StackReason
    {
        FirstGuess = 0,
        SecondGuess,
        UnitPropagation,
        PureLiteralElimination
    }

    readonly CancellationToken _cancellationToken;
    readonly DpllMode _mode;

    readonly Variable[] _variables;
    readonly Constraint[] _constraints;


    /// <summary>
    /// Keeps track of already tried variables at
    /// a specified stage, so we don't try the same
    /// combination twice.
    /// The index into this array is typically the
    /// _stack.Count.
    /// </summary>
    readonly HashSet<int>[] _testedVariables;

    /// <summary>
    /// Indicates the variable that we just tested
    /// and want to switch. Only if this is <c>null</c>
    /// we choose a new variable by heuristic.
    /// </summary>
    Variable? _currentCandidate;

    /// <summary>
    /// Keeps track of unit clauses found
    /// during propagation.
    /// </summary>
    readonly Queue<Constraint> _unitConstraints = [];

    /// <summary>
    /// Keeps track of pure literals found
    /// during propagation.
    /// </summary>
    readonly Queue<Variable> _pureLiterals;

    /// <summary>
    /// The stack stores our decisions and if they
    /// were made to guess a variable or if it was
    /// an optimization after guessing.
    /// This is required to know how far to track
    /// back when a final state was reached.
    /// guessed = 0 means it was an optimization and
    /// we should go further down the stack.
    /// guessed = 1 means it was the first guess and
    /// we should start with the inverse Sense again.
    /// guessed = 2 means it already was the final guess
    /// and we need to go even further down the stack.
    /// </summary>
    readonly Stack<(Variable variable, StackReason reason)> _stack = [];

    /// <summary>
    /// <c>true</c> if an empty clause was found during propagation.
    /// </summary>
    bool _foundEmptyConstraint;

    /// <summary>
    /// The number of still active clauses after propagation.
    /// </summary>
    int _numberOfActiveConstraints;

    public DpllProcessor(Problem problem, DpllMode mode, CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _mode = mode;

        // Note that literals are actually 1-indexed. We switch to 0-indexing here
        // and need to restore that when yielding solutions.
        _variables = Enumerable.Range(0, problem.NumberOfLiterals).Select(i => new Variable(i)).ToArray();

        // create storage variables tried
        // at a certain stage.
        _testedVariables = Enumerable.Range(0, _variables.Length).Select(_ => new HashSet<int>()).ToArray();

        //
        // Build up constraints and set connections to variables.
        // Literals are reduced to appear only once per constraint/sense.
        // Constraints that are obviously true (containing 'a -a') will
        // be skipped.
        // Unit constraints will already be enqueued to be propagated
        // after the first try.
        //
        var constraints = new List<Constraint>();
        var unitClauseVariables = new List<Variable>();
        foreach (var clause in problem.Clauses)
        {
            var literals = clause.Literals.DistinctBy(l => (l.Id, l.Sense)).ToArray();
            if (literals.GroupBy(l => l.Id).Any(g => g.Skip(1).Any())) continue; // trivial cases 'a -a'

            var constraint = new Constraint(constraints.Count);
            foreach (var literal in literals)
            {
                var variable = _variables[literal.Id-1];
                if (literal.Sense)
                {
                    variable.Positives.Add(constraint);
                    variable.NumberOfActivePositives++;
                    constraint.Positives.Add(variable);
                }
                else
                {
                    variable.Negatives.Add(constraint);
                    variable.NumberOfActiveNegatives++;
                    constraint.Negatives.Add(variable);
                }
            }

            constraint.NumberOfActivePositives = constraint.Positives.Count;
            constraint.NumberOfActiveNegatives = constraint.Negatives.Count;

            var numberOfActiveLiterals = constraint.NumberOfActivePositives + constraint.NumberOfActiveNegatives;
            if (numberOfActiveLiterals == 1)
                _unitConstraints.Enqueue(constraint);
            if (numberOfActiveLiterals == 0)
                _foundEmptyConstraint = true;
            constraints.Add(constraint);
        }

        _constraints = [.. constraints];
        _numberOfActiveConstraints = _constraints.Length;
        _pureLiterals = _mode == DpllMode.DecisionOnly ? new(_variables.Where(variable => variable.NumberOfActiveNegatives + variable.NumberOfActivePositives == 1)) : [];
    }

    public bool TryNextVariable(out Literal[][]? solutions)
    {
        solutions = null;

        if (_numberOfActiveConstraints > 0 && !_foundEmptyConstraint)
        {
            var variable = _currentCandidate ?? GetNextCandidate();
            var reason = _currentCandidate is null ? StackReason.FirstGuess : StackReason.SecondGuess;
            _currentCandidate = null;
            Propagate(variable, reason);
            UnitPropagation();
            PureLiteralElimination();
        }

        if (_numberOfActiveConstraints == 0)
        {
            solutions = CreateCurrentSolutions().ToArray();
            return true;
        }

        return _foundEmptyConstraint;
    }

    /// <summary>
    /// Compiles the current selection into 
    /// a Literal array for the caller.
    /// NOTE: we reduced 1-indexed literal IDs
    /// to 0-indexed and need to reverse it now.
    /// </summary>
    /// <returns>The solution as Literal[].</returns>
    IEnumerable<Literal[]> CreateCurrentSolutions()
    {
        if (_mode == DpllMode.DecisionOnly)
        {
            yield return _variables.Select(variable => new Literal(variable.Index+1, variable.Sense)).ToArray();
            yield break;
        }

        foreach (var variable in _variables.Where(variable => !variable.Fixed)) variable.Sense = false;
        yield return _variables.Select(variable => new Literal(variable.Index+1, variable.Sense)).ToArray();

        // TODO: enumerate all possible solutions for still free variables
    }

    /// <summary>
    /// The stack knows which variable was set and wether that
    /// was a guess or an optimization.
    /// We can know restore the data to the situation before
    /// that change.
    /// We go back until the last guessed variable. If it was
    /// guessed once, we now switch it and try again, otherwise 
    /// we try to go back further.
    /// If we reached the bottom of the stack without any more
    /// guessed variables, the search has finished.
    /// </summary>
    /// <returns><c>true</c> if we changed a variable and
    /// start over, <c>false</c> if not and we finished.</returns>
    public bool Backtrack()
    {
        while(_stack.Count > 0)
        {
            // Clear tracking of variables
            // tested after the variable on stack
            if (_stack.Count < _testedVariables.Length) // no tracking slot for last variable
                _testedVariables[_stack.Count].Clear();

            // Get variable from stack
            // and depropagate it
            var (variable, reason) = _stack.Pop();
            Depropagate(variable);

            // Check if we try this variable 
            // the other way round now.
            if (reason == StackReason.FirstGuess)
            {
                variable.Sense = !variable.Sense;
                _currentCandidate = variable;
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Propagates a variable change. 
    /// The constraitns that will be satisfied by this change
    /// will be removed and their <see cref="Constraint.RemovalIndex"/>
    /// will be set to the current stack index.
    /// The variable is "removed" from all constraints where it 
    /// evaluates to false by reducing the respective counters.
    /// When we encounter empty constraints, unti constraints or
    /// pure literals we enqueue them for treatment later.
    /// </summary>
    /// <param name="variable">The variable to fix.</param>
    /// <param name="reason">Indicates wether this is a guess or an optimization.</param>
    void Propagate(Variable variable, StackReason reason)
    {
        variable.Fixed = true;
        _testedVariables[_stack.Count].Add(variable.Index);

        //
        // Go through the constraints that will be satisfied
        // and remove them.
        //
        foreach (var constraint in (variable.Sense ? variable.Positives : variable.Negatives).Where(constraint => constraint.RemovalIndex < 0))
        {
            constraint.RemovalIndex = _stack.Count;

            //
            // Decrease the respective counters in 
            // the still active variables.
            // If this results in a pure literal, enqueue it.
            //
            foreach (var v in constraint.Positives.Where(v => !v.Fixed))
            {
                if (--v.NumberOfActivePositives == 0 && _mode == DpllMode.DecisionOnly)
                    _pureLiterals.Enqueue(v);
            }
            foreach (var v in constraint.Negatives.Where(v => !v.Fixed))
            {
                if (--v.NumberOfActiveNegatives == 0 && _mode == DpllMode.DecisionOnly)
                    _pureLiterals.Enqueue(v);
            }

            _numberOfActiveConstraints--;
        }

        //
        // Remove the variable from constraints it cannot satisfy.
        // Check the affected constraints for unit or empty ones.
        //
        foreach (var constraint in (variable.Sense ? variable.Negatives : variable.Positives).Where(constraint => constraint.RemovalIndex < 0))
        {
            if (variable.Sense)
                constraint.NumberOfActiveNegatives--;
            else
                constraint.NumberOfActivePositives--;

            var numberOfActiveLiterals = constraint.NumberOfActiveNegatives + constraint.NumberOfActivePositives;
            if (numberOfActiveLiterals == 0)
                _foundEmptyConstraint = true;
            if (numberOfActiveLiterals == 1)
                _unitConstraints.Enqueue(constraint);
        }

        _stack.Push((variable, reason));
    }

    /// <summary>
    /// Reverses a propagation by reactivating the
    /// constraints that were satisfied during this 
    /// step and updating the counters of contained
    /// variables.
    /// This must be done in reverse order of <see cref="Propagate"/>
    /// to keep the counters consistent.
    /// </summary>
    /// <param name="variable">The variable to depropagate.</param>
    void Depropagate(Variable variable)
    {
        //
        // Re-add variable to the clauses
        //
        foreach (var constraint in (variable.Sense ? variable.Negatives : variable.Positives).Where(constraint => constraint.RemovalIndex < 0))
        {
            if (variable.Sense)
                constraint.NumberOfActiveNegatives++;
            else
                constraint.NumberOfActivePositives++;
        }

        // Reactivate constraints that were
        // satisfied by this variable's propagation.
        //
        foreach (var constraint in (variable.Sense ? variable.Positives : variable.Negatives).Where(constraint => constraint.RemovalIndex == _stack.Count))
        {
            constraint.RemovalIndex = -1;

            // update counters in referenced variables
            foreach (var v in constraint.Positives.Where(v => !v.Fixed))
                v.NumberOfActivePositives++;
            foreach (var v in constraint.Negatives.Where(v => !v.Fixed))
                v.NumberOfActiveNegatives++;

            _numberOfActiveConstraints++;
        }

        //
        // Do this at the end, because we do it
        // at the beginning of propagation. So
        // The behaviour (which counters are updated)
        // stays consistent.
        //
        variable.Fixed = false;
        _foundEmptyConstraint = false;
    }

    /// <summary>
    /// Tries to find a good next candiate variable.
    /// NOTE: Only call this when <see cref="_currentCandidate"/> is <c>null</c>.
    /// If <c>_currentCandidate</c> is not <c>null</c>, take this for running
    /// the second branch of this variable.
    /// </summary>
    /// <returns></returns>
    Variable GetNextCandidate()
    {
        //
        // Choose the most frequently used variable that
        // was not already tested under the current prefix.
        //     
        var candidates = _variables.Where(variable => !variable.Fixed && !_testedVariables[_stack.Count].Contains(variable.Index));

        var chosenVariable = candidates.MaxBy(variable => variable.NumberOfActiveNegatives + variable.NumberOfActivePositives)!;
        chosenVariable.Sense = chosenVariable.NumberOfActivePositives > chosenVariable.NumberOfActiveNegatives; // to eliminate the most clauses
        return chosenVariable;
    }

    /// <summary>
    /// Unit propgation: During initialization or propagation we remembered 
    /// clauses that contain only a single. literal. This literal can be fixed 
    /// according to its sense and all clauses where the literal occurs in the 
    /// same sense can be removed and all occurences of this literal in a different
    /// sense can be removed from the respective clauses.
    /// </summary>
    /// <returns><c>true</c> if changes were made, <c>false</c> if not.</returns>
    void UnitPropagation()
    {
        while(!(_foundEmptyConstraint || _numberOfActiveConstraints == 0) && _unitConstraints.TryDequeue(out var constraint))
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (constraint.RemovalIndex > -1) continue; // constraint was already removed in between
            if (constraint.NumberOfActivePositives + constraint.NumberOfActiveNegatives != 1) continue; // constraint changed in between and is no unit constraint anymore

            var positive = constraint.NumberOfActivePositives == 1;

            var variable =
                (positive ? constraint.Positives : constraint.Negatives).Single(v => !v.Fixed);

            variable.Sense = positive;
            Propagate(variable, StackReason.UnitPropagation);
        }
    }

    /// <summary>
    /// Pure Literal Elimination: if a literal only occures in one sense, we can 
    /// safely set it to its sense to satisfy the clauses it's contained in.
    /// </summary>
    /// <returns><c>true</c> if clauses were changed, <c>false</c> if not (or clauses were removed).</returns>
    void PureLiteralElimination()
    {
        if (_mode != DpllMode.DecisionOnly) return;

        while (!(_foundEmptyConstraint || _numberOfActiveConstraints == 0) && _pureLiterals.TryDequeue(out var variable))
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (variable.Fixed) continue;
            variable.Sense = variable.NumberOfActiveNegatives == 0;
            Propagate(variable, StackReason.PureLiteralElimination);
        }
    }
}
