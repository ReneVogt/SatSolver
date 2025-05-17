using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Revo.SatSolver;
using SatSolverTests;

namespace SatSolverBenchmark;

[SimpleJob(warmupCount: 1, iterationCount: 5)]

public class Benchmark
{
    readonly SatSolver.Options _options = TestOptions.Default;

    (string Name, Problem Problem)[]? _satProblems;
    (string Name, Problem Problem)[]? _unsatProblems;

    [GlobalSetup]
    public void Setup()
    {
        (_satProblems, _unsatProblems) = ProblemLoader.LoadProblems();
    }

    [Benchmark(Description = "SAT")]
    public void SAT()
    {
        foreach (var (name, problem) in _satProblems!)
        {
            var solution = SatSolver.Solve(problem, _options) ?? throw new Exception($"Problem {name} could not be solved.");
            SolutionValidator.Validate(problem, solution);
        }
    }
    [Benchmark(Description = "UNSAT")]
    public void UNSAT()
    {
        foreach (var (name, problem) in _unsatProblems!)
        {
            var solution = SatSolver.Solve(problem, _options);
            if (solution is not null)
                throw new Exception($"Problem {name} should not have a solution.");
        }
    }

    public static void Run()
    {
        BenchmarkRunner.Run<Benchmark>();
    }
}
