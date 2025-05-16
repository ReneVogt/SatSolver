using static System.Console;

using Revo.SatSolver.Parsing;
using System.Diagnostics;
using Revo.SatSolver;
using SatSolverTests;
using static Revo.SatSolver.SatSolver;

var testOptions = new Options()
{
    OnlyPoorMansVSIDS = true,

    VariableActivityDecayFactor = 0.9995,

    ClauseActivityDecayFactor = 0.999,
    LiteralBlockDistanceMaximum = 8,

    ClauseDeletion = new()
    {
        LiteralBlockDistanceToKeep = 2,
        OriginalClauseCountFactor = 5,
        RatioToDelete = 0.5,
        LiteralBlockDistanceThreshold = 1.3,
        PropagationRateThreshold = 0.5
    },

    Restart = new()
    {
        Interval = null,
        Luby = false,
        LiteralBlockDistanceThreshold = null,
        PropagationRateThreshold = null
    },

    LiteralBlockDistanceTracking = new()
    {
        Decay = 0.999,
        RecentCount = 100
    },

    PropagationRateTracking = new()
    {
        ConflictInterval = 500,
        Decay = 0.999,
        SampleSize = 50
    }
};

CursorVisible = false;
var elapsed = TimeSpan.Zero;
try
{
    var satProblems = Directory.GetFiles("SAT", "*.cnf")
        .Select(file => (file, problem: DimacsParser.Parse(File.ReadAllText(file)).Single())).OrderBy(x => x.file).ToArray();
    var unsatProblems = Directory.GetFiles("UNSAT", "*.cnf")
        .Select(file => (file, problem: DimacsParser.Parse(File.ReadAllText(file)).Single())).OrderBy(x => x.file).ToArray();

    var textLength = satProblems.Concat(unsatProblems).Select(x => x.file.Length).Max();

    WriteLine($"Found {satProblems.Length} SAT and {unsatProblems.Length} UNSAT problems.");

    Write("SAT:   ");
    Solve(satProblems, true);
    SetCursorPosition(7, 1);
    WriteLine($"{elapsed,-50:mm\\:ss\\.fff}");
    Write("UNSAT: ");
    var satTime = elapsed;
    elapsed = TimeSpan.Zero;
    Solve(unsatProblems, false);
    SetCursorPosition(7, 2);
    WriteLine($"{elapsed,-50:mm\\:ss\\.fff}");
    WriteLine($"TOTAL: {elapsed+satTime:mm\\:ss\\.fff}");
}
finally
{
    CursorVisible = true;
}

void Solve((string file, Problem problem)[] problems, bool sat)
{
    (_, var top) = GetCursorPosition();
    for (var i=0; i<problems.Length; i++)
    {
        SetCursorPosition(7, top);
        var dotCount = 20 * i / problems.Length;
        var dots = new string('.', dotCount);
        var spaces = new string(' ', 20 - dotCount);
        var estimated = i > 0 ? (double)problems.Length / i * elapsed : TimeSpan.Zero;
        Write($"{i}/{problems.Length} [{dots}{spaces}] {elapsed:mm\\:ss\\.fff} {estimated:mm\\:ss\\:fff}");
        var watch = Stopwatch.StartNew();
        var solution = SatSolver.Solve(problems[i].problem, testOptions);
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