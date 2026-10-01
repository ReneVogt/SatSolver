using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Perfolizer.Mathematics.OutlierDetection;
using Revo.SatSolver;
using Revo.SatSolver.Parsing;
using SatSolverTests;

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

    readonly SatSolverOptions _options = SatSolverOptions.CDCL;

    Problem _problem = null!;

    public sealed record BenchmarkCase(string FilePath, bool IsSatisfiable)
    {
        public override string ToString() => FilePath;
    }

    // Replace these six paths with the selected CNF files and keep the selection fixed.
    public static IEnumerable<BenchmarkCase> Cases =>
    [
        new("SAT/easy.cnf", true),
        new("SAT/medium.cnf", true),
        new("SAT/hard.cnf", true),
        new("UNSAT/easy.cnf", false),
        new("UNSAT/medium.cnf", false),
        new("UNSAT/hard.cnf", false)
    ];

    [ParamsSource(nameof(Cases))]
    public BenchmarkCase Case { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        _problem = DimacsParser.Parse(File.ReadAllText(Case.FilePath)).Single();

        // Validate once outside the measurement; Solve creates a fresh solver each time.
        var solution = Solve();
        if (Case.IsSatisfiable)
        {
            if (solution is null)
                throw new Exception($"Problem {Case.FilePath} could not be solved.");
            SolutionValidator.Validate(_problem, solution);
        }
        else if (solution is not null)
            throw new Exception($"Problem {Case.FilePath} should not have a solution.");
    }

    [Benchmark]
    public Literal[]? Solve() => SatSolverFactory.EnumerateSolutions(_problem, _options).FirstOrDefault();

    public static void Run()
    {
        BenchmarkRunner.Run<SatSolverBenchmark>();
    }
}
