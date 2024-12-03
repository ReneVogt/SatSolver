using Revo.SatSolver.DPLL;
using Revo.SatSolver.Properties;
using System.Diagnostics.CodeAnalysis;

namespace Revo.SatSolver;

public sealed class SatSolver
{
    readonly CancellationToken _cancellationToken;
    readonly DpllProcessor _state;


    SatSolver(Problem problem, DpllMode mode, CancellationToken cancellationToken)
    { 
        _ = problem ?? throw new ArgumentNullException(nameof(problem));
        _cancellationToken = cancellationToken;

        if (problem.NumberOfLiterals < 1)
            throw new ArgumentException(Resources.SatSolverArgumentException_NumberOfLiterals, nameof(problem));
        if (problem.Clauses.SelectMany(clause => clause.Literals.Select(literal => Math.Abs(literal.Id))).Any(id => id < 1 || id > problem.NumberOfLiterals))
            throw new ArgumentException(Resources.SatSolverArgumentException_InvalidLiterals, nameof(problem));

        _state = new DpllProcessor(problem, mode, _cancellationToken);
    }

    IEnumerable<Literal[]> Solve()
    {
        do
        {
            _cancellationToken.ThrowIfCancellationRequested();
            
            Literal[][]? solutions;
            while(!_state.TryNextVariable(out solutions)) { _cancellationToken.ThrowIfCancellationRequested(); }

            if (solutions is not null)
                foreach(var solution in solutions)
                    yield return solution;

        } while (_state.Backtrack());
    }

    bool IsSatisfiable([NotNullWhen(true)] out Literal[][]? solutions)
    {
        do
        {
            _cancellationToken.ThrowIfCancellationRequested();

            while (!_state.TryNextVariable(out solutions)) { _cancellationToken.ThrowIfCancellationRequested(); }

            if (solutions is not null)
                return true;

        } while (_state.Backtrack());

        return false;
    }


    /// <summary>
    /// Tries to enumerate solutions that satisfy the given <paramref name="problem"/>.
    /// Multiple solutions will only be produced when the returned sequence is enumerated.
    /// </summary>
    /// <param name="problem">The <see cref="Problem"/> to satisfy.</param>
    /// <returns>A sequence of solutions. The sequence is empty if no solution was found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> was <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The problem contains either invalid literal IDs or no literals at all.</exception>
    public static IEnumerable<Literal[]> Solve(Problem problem, DpllMode mode = DpllMode.AllSolutions, CancellationToken cancellationToken = default)
    {
        var solver = new SatSolver(problem, mode, cancellationToken);
        return solver.Solve();
    }
    public static bool IsSatisfiable(Problem problem, CancellationToken cancellationToken = default) => IsSatisfiable(problem, out _, cancellationToken);
    
    public static bool IsSatisfiable(Problem problem, [NotNullWhen(true)] out Literal[][]? solutions, CancellationToken cancellationToken = default)
    {
        var solver = new SatSolver(problem, DpllMode.DecisionOnly, cancellationToken);
        return solver.IsSatisfiable(out solutions);
    }
}
