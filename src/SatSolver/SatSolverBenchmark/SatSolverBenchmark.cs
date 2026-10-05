using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Perfolizer.Mathematics.OutlierDetection;
using Revo.SatSolver;
using Revo.SatSolver.Parsing;
using System.Diagnostics;
using TestHelpers;

namespace SatSolverBenchmark;

[Config(typeof(SolverBenchmarkConfig))]
public class SatSolverBenchmark
{
    sealed class SolverBenchmarkConfig : ManualConfig
    {
        public SolverBenchmarkConfig()
        {
            // Leave WarmupCount and IterationCount unset for automatic stopping criteria.
            AddJob(Job.Default
                .WithId("Solver")
                .WithLaunchCount(3)
                .WithMinIterationCount(15)
                .WithMaxIterationCount(50)
                .WithMaxRelativeError(0.02)
                .WithOutlierMode(OutlierMode.DontRemove)
                .WithGcServer(true)
                .WithGcConcurrent(false)
                .WithAffinity(0xFFF));                
        }
    }

    static readonly SatSolverOptions _options = new()
    {
        //LiteralBlockDistanceTracking = new(),
        //PropagationRateTracking = new(),
        MaximumLiteralBlockDistance = 100,
        ConstraintDeletion = new()
        {
            ConflictInterval = 5000,
            LiteralBlockDistanceToKeep = 3,
            RatioToDelete = 0.5,
            ReduceOnRestart = false,
            OriginalConstraintCountFactor = 4
        },
        Restart = new()
        {
            ByLiteralBlockDistance = false,
            ByPropagationRate = false,
            Interval = null,
            Luby = false
        }
    };

    Problem _problem = null!;

    public sealed record BenchmarkCase(string Name, string FilePath, bool IsSatisfiable)
    {
        public override string ToString() => FilePath;
    }

    public static IEnumerable<BenchmarkCase> Cases => //new[] { new BenchmarkCase("Test", "cnf\\UNSAT\\2bitadd_10.cnf", false) };
        Directory.EnumerateFiles("cnf\\SAT", "*.cnf").Select(path => new BenchmarkCase(Path.GetFileNameWithoutExtension(path), path, true))
        .Concat(Directory.EnumerateFiles("cnf\\UNSAT", "*.cnf").Select(path => new BenchmarkCase(Path.GetFileNameWithoutExtension(path), path, false)));

    [ParamsSource(nameof(Cases))]
    public BenchmarkCase Case { get; set; } = null!;

    [GlobalSetup]
    public void Setup() => _problem = DimacsParser.Parse(File.ReadAllText(Case.FilePath)).Single();

    [Benchmark]
    public Literal[]? Solve() => SatSolverFactory.EnumerateSolutions(_problem, _options).FirstOrDefault();

    public static void Run()
    {
        BenchmarkRunner.Run<SatSolverBenchmark>();
    }

    public static void Validate()
    {
        var cases = Cases.ToArray();
        Console.Clear();
        Console.WriteLine(string.Join(Environment.NewLine, cases.Select(c => $"{c.Name, -25}...")));
        var tasks = cases.Select(entry => Task.Run(() =>
        {
            var (isValid, elapsed) = Validate(entry);
            return (entry, isValid, elapsed);
        })).ToList();
        while (tasks.Count > 0)
        {
            var finishedTask = Task.WhenAny(tasks).Result;
            tasks.Remove(finishedTask);
            var (entry, isValid, elapsed) = finishedTask.Result;
            var index = Array.IndexOf(cases, entry);
            Console.SetCursorPosition(25, index);
            Console.ForegroundColor = isValid ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write(elapsed);
            Console.ResetColor();
        }
        Console.SetCursorPosition(0, cases.Length+1);
    }
    static (bool, TimeSpan) Validate(BenchmarkCase entry)
    {
        var problem = DimacsParser.Parse(File.ReadAllText(entry.FilePath)).Single();

        var watch = Stopwatch.StartNew();
        var solution = SatSolverFactory.EnumerateSolutions(problem, _options).FirstOrDefault();
        watch.Stop();
        if (entry.IsSatisfiable)
        {
            if (solution is null) return (false, watch.Elapsed);
            try
            {
                SolutionValidator.Validate(problem, solution);
                return (true, watch.Elapsed);
            }
            catch
            {
                return (false, watch.Elapsed);
            }

        }
        else return (solution is null, watch.Elapsed);
    }
}
