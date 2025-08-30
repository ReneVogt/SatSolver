
using Revo.SatSolver;
using SatSolverTests;
using System.Diagnostics;

using static System.Console;

namespace SatSolverBenchmark;
static class SudokuBenchmark
{
    public static void Run()
    {
        Clear();
        CursorVisible = false;
        var elapsed = TimeSpan.Zero;
        try
        {
            var problem = ProblemLoader.LoadSudoku();

            Write("SOLVE:   ");
            Solve(problem);
            SetCursorPosition(7, 0);
            WriteLine($"{elapsed,-50:mm\\:ss\\.ff}");
        }
        finally
        {
            CursorVisible = true;
        }

        void Solve(Problem problem)
        {
            const int durations = 10;
            (_, var top) = GetCursorPosition();
            using var enumerator = SatSolver.EnumerateSolutions(problem, SatSolverOptions.CDCL).GetEnumerator();
            for (var i = 0; i<durations; i++)
            {
                SetCursorPosition(7, top);
                var dotCount = 20 * i / durations;
                var dots = new string('.', dotCount);
                var spaces = new string(' ', 20 - dotCount);
                var estimated = i > 0 ? (double)durations / i * elapsed : TimeSpan.Zero;
                Write($"{i}/{durations} [{dots}{spaces}] {elapsed:mm\\:ss\\.ff} {estimated:mm\\:ss\\:ff}");
                var watch = Stopwatch.StartNew();
                if (!enumerator.MoveNext()) throw new Exception("No more solutions!");
                var solution = enumerator.Current;
                watch.Stop();
                elapsed += watch.Elapsed;
                SolutionValidator.Validate(problem, solution);
                //ValidateSudoku(solution);
            }
        }
    }
}
