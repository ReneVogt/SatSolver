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

    static readonly SatSolverOptions _options = SatSolverOptions.CDCL with 
    { 
        Restart = new() 
        { 
            ByLiteralBlockDistance = false, 
            ByPropagationRate = false, 
            Interval = null, 
            Luby = false 
        },
        MaximumLiteralBlockDistance = 30
    };

    Problem _problem = null!;

    public sealed record BenchmarkCase(string FilePath, bool IsSatisfiable)
    {
        public override string ToString() => FilePath;
    }

    // Replace these six paths with the selected CNF files and keep the selection fixed.
    public static IEnumerable<BenchmarkCase> Cases =>
    [
        new("rnd3-easy.cnf", true),
        new("rnd3-medium.cnf", true),
        new("rnd3-hard.cnf", true),
        new("rnd3u-easy.cnf", false),
        new("rnd3u-medium.cnf", false),
        new("rnd3u-hard.cnf", false),
        new("2bitadd_10.cnf", true),
        new("2bitadd_11.cnf", true),
        new("2bitadd_12.cnf", true),
        new("2bitcomp_5.cnf", true),
        new("2bitmax_6.cnf", true),
        new("3bitadd_31.cnf", true),
        new("3bitadd_32.cnf", true),
        new("3blocks.cnf", true),
        new("4blocks.cnf", true),
        new("4blocksb.cnf", true),
        new("e0ddr2-10-by-5-1.cnf", true),
        new("e0ddr2-10-by-5-4.cnf", true),
        new("enddr2-10-by-5-1.cnf", true),
        new("enddr2-10-by-5-8.cnf", true),
        new("ewddr2-10-by-5-1.cnf", true),
        new("ewddr2-10-by-5-8.cnf", true)
    ];


    [ParamsSource(nameof(Cases))]
    public BenchmarkCase Case { get; set; } = null!;

    [GlobalSetup]
    public void Setup() => _problem = DimacsParser.Parse(File.ReadAllText(Path.Combine("cnf", Case.FilePath))).Single();

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
        Console.WriteLine(string.Join(Environment.NewLine, cases.Select(c => $"{c.FilePath, -25}...")));
        var tasks = cases.Select(entry => Task.Run(() =>
        {
            var (isValid, elapsed) = Validate(entry);
            return (entry, isValid, elapsed);
        })).ToList();
        while(tasks.Count > 0)
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
    }
    static (bool, TimeSpan) Validate(BenchmarkCase entry)
    {
        var problem = DimacsParser.Parse(File.ReadAllText(Path.Combine("cnf", entry.FilePath))).Single();

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
