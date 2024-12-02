using Revo.SatSolver.DPLL;
using Revo.SatSolver.Properties;

namespace Revo.SatSolver;

public sealed class SatSolver
{
    readonly CancellationToken _cancellationToken;
    readonly DpllState _state;


    SatSolver(Problem problem, CancellationToken cancellationToken)
    { 
        _ = problem ?? throw new ArgumentNullException(nameof(problem));
        _cancellationToken = cancellationToken;

        if (problem.NumberOfLiterals < 1)
            throw new ArgumentException(Resources.SatSolverArgumentException_NumberOfLiterals, nameof(problem));
        if (problem.Clauses.SelectMany(clause => clause.Literals.Select(literal => Math.Abs(literal.Id))).Any(id => id < 1 || id > problem.NumberOfLiterals))
            throw new ArgumentException(Resources.SatSolverArgumentException_InvalidLiterals, nameof(problem));

        _state = new DpllState(problem, _cancellationToken);
    }

    IEnumerable<Literal[]> Solve()
    {
        do
        {
            _cancellationToken.ThrowIfCancellationRequested();
            
            Literal[]? solution;
            while(!_state.TryNextVariable(out solution)) { _cancellationToken.ThrowIfCancellationRequested(); }

            if (solution is not null)
                yield return solution;

        } while (_state.Backtrack());
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
