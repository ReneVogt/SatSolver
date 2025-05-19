using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Revo.SatSolver.DataStructures;


namespace SatSolverBenchmark;

[SimpleJob(warmupCount: 100, iterationCount: 10000)]

public class CandidateHeapBenchmark
{
    readonly Variable[] _variables = [.. Enumerable.Range(0, 10240).Select(i => new Variable(i) { Activity = i + 2 * (i & 1) - 1})];

    [IterationSetup]
    public void Setup()
    {
        foreach(var variable in _variables)
        {
            variable.Activity = 0;
            variable.Sense = null;
        }
    }

    [Benchmark(Description = "CandidateHeap")]

    public void Test()
    {
        var sut = new CandidateHeap(_variables);
        var activity = 0;
        foreach(var variable in _variables)
        {            
            variable.Activity = ++activity;
            sut.Enqueue([variable]);
            if (activity % 10 == 0) variable.Sense = true;
        }

        activity = 0;
        for (var i=0; i< _variables.Length; i++)
        {
            var variable = sut.Dequeue()!;
            variable.Activity -= ++activity;
            variable.Sense = null;
            sut.Enqueue([variable]);
        }

        var temp = new List<Variable?>();
        Variable? v;
        do
        {
            v = sut.Dequeue();
            temp.Add(v);
        } while (v is not null);

        var sum = temp.Sum(v => v?.Activity ?? 0);
        GC.KeepAlive(sum);
        GC.KeepAlive(temp);
    }

    public static void Run()
    {
        BenchmarkRunner.Run<CandidateHeapBenchmark>();
    }
}
