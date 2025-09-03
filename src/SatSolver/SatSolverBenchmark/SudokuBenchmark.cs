
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
            const int durations = 100;
            //var options = SatSolverOptions.DPLL;
            var options = SatSolverOptions.Sudoku;

            (_, var top) = GetCursorPosition();
            using var enumerator = SatSolverFactory.EnumerateSolutions(problem, options).GetEnumerator();
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
                ValidateSudoku(solution);
            }
        }
    }

    static void ValidateSudoku(Literal[] solution)
    {
        var numbers = solution.Where(l => l.Sense).GroupBy(l => (l.Id-1)/9).ToDictionary(g => g.Key, g => g.Single());
        var board = numbers.OrderBy(k => k.Key).Select(k => ((k.Value.Id - 1) % 9) + 1).ToArray();
        if (board.Length != 81) throw new Exception("Strange things happened!");
        if (!board.All(n => n >=1 && n<=9)) throw new Exception("Invalid numbers!");
        if (!Enumerable.Range(0, 9).All(column => Enumerable.Range(0, 9).Select(row => board[column*9+row]).Distinct().Count() == 9))
            throw new Exception("Duplicates in a column!");
        if (!Enumerable.Range(0, 9).All(row => Enumerable.Range(0, 9).Select(column => board[column*9+row]).Distinct().Count() == 9))
            throw new Exception("Duplicates in a row!");

        var boxOffsets = new[] { 0, 1, 2, 9, 10, 11, 18, 19, 20 };
        var boxStarts = new[] { 0, 3, 6, 27, 30, 33, 54, 57, 60 };
        if (!Enumerable.Range(0, 9).All(box => Enumerable.Range(0, 9).Select(index => board[boxStarts[box] + boxOffsets[index]]).Distinct().Count() == 9))
            throw new Exception("Duplicates in a box!");
    }
}
