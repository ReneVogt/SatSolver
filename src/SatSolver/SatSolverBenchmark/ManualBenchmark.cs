using Revo.SatSolver;
using SatSolverTests;
using System.Diagnostics;

using static System.Console;

namespace SatSolverBenchmark;
static class ManualBenchmark
{
    public static void Run()
    {
        Clear();
        CursorVisible = false;
        var elapsed = TimeSpan.Zero;
        try
        {
            var (satProblems, unsatProblems) = ProblemLoader.LoadProblems();

            var textLength = satProblems.Concat(unsatProblems).Select(x => x.Name.Length).Max();

            WriteLine($"Found {satProblems.Length} SAT and {unsatProblems.Length} UNSAT problems.");

            Write("SAT:   ");
            Solve(satProblems, true);
            SetCursorPosition(7, 1);
            WriteLine($"{elapsed,-50:mm\\:ss\\.ff}");
            Write("UNSAT: ");
            var satTime = elapsed;
            elapsed = TimeSpan.Zero;
            Solve(unsatProblems, false);
            SetCursorPosition(7, 2);
            WriteLine($"{elapsed,-50:mm\\:ss\\.ff}");
            WriteLine($"TOTAL: {elapsed+satTime:mm\\:ss\\.ff}");
        }
        finally
        {
            CursorVisible = true;
        }

        void Solve((string file, Problem problem)[] problems, bool sat)
        {
            (_, var top) = GetCursorPosition();
            for (var i = 0; i<problems.Length; i++)
            {
                SetCursorPosition(7, top);
                var dotCount = 20 * i / problems.Length;
                var dots = new string('.', dotCount);
                var spaces = new string(' ', 20 - dotCount);
                var estimated = i > 0 ? (double)problems.Length / i * elapsed : TimeSpan.Zero;
                Write($"{i}/{problems.Length} [{dots}{spaces}] {elapsed:mm\\:ss\\.ff} {estimated:mm\\:ss\\:ff}");
                var watch = Stopwatch.StartNew();
                var solution = SatSolver.Solve(problems[i].problem, SatSolver.Options.CDCL);
                watch.Stop();
                elapsed += watch.Elapsed;
                if (sat)
                {
                    if (solution is null)
                        throw new Exception($"{problems[i].file} could not be solved.");
                    SolutionValidator.Validate(problems[i].problem, solution);
                }
                else if (solution is not null)
                    throw new Exception($"{problems[i].file} was solved although it was declared as UNSAT.");
            }
        }
    }
}
