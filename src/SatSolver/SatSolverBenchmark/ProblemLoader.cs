using Revo.SatSolver;
using Revo.SatSolver.Parsing;

namespace SatSolverBenchmark;
static class ProblemLoader
{
    public static ((string Name, Problem Problem)[] Sat, (string Name, Problem Problem)[] Unsat) LoadProblems()
    {
        var satProblems = Directory.GetFiles("SAT", "*.cnf")
            .Select(file => (file, problem: DimacsParser.Parse(File.ReadAllText(file)).Single())).OrderBy(x => x.file).ToArray();
        var unsatProblems = Directory.GetFiles("UNSAT", "*.cnf")
            .Select(file => (file, problem: DimacsParser.Parse(File.ReadAllText(file)).Single())).OrderBy(x => x.file).ToArray();

        return (satProblems, unsatProblems);
    }
    public static ((string Name, Problem Problem)[] Sat, (string Name, Problem Problem)[] Unsat) LoadTestProblems()
    {
        var problem = DimacsParser.Parse(File.ReadAllText("SAT\\uf250-01.cnf")).Single();
        var satProblems = Enumerable.Range(0, 10)
            .Select(i => ($"Test{i}", problem)).ToArray();

        return (satProblems, []);
    }
}
