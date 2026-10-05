using Revo.SatSolver;
using Revo.SatSolver.Parsing;
using TestHelpers;
using static Revo.SatSolver.SatSolverFactory;
using static SatSolverTests.Problems;

namespace SatSolverTests;

public sealed partial class SatSolverTests
{
    [Fact]
    [Trait("Category", "Simple Cases")]
    public void EnumerateSolutions_NoLiterals_EmptySolution()
    {
        var solution = new Problem(0, []).EnumerateSolutions().Single();
        Assert.NotNull(solution);
        Assert.Empty(solution);
    }
    [Fact]
    [Trait("Category", "Simple Cases")]
    public void EnumerateSolutions_EmptyClause_ArgumentException()
    {
        var problem = new Problem(2, [new([1, 2]), new([1]), new([]), new([2])]);
        Assert.Throws<ArgumentException>(() => problem.EnumerateSolutions());        
    }

    [Fact]
    [Trait("Category", "Simple Cases")]
    public void EnumerateSolutions_NoClauses_AllSolutions()
    {
        var solutions = new Problem(3, []).EnumerateSolutions(SatSolverOptions.Default).ToArray();
        var clauses = solutions.Select(s => new Clause(s)).OrderBy(c => c).ToArray();
        Assert.Equal([
            [-1, -2, -3],
                [-1, -2, 3],
                [-1, 2, -3],
                [-1, 2, 3],
                [1, -2, -3],
                [1, -2, 3],
                [1, 2, -3],
                [1, 2, 3]], clauses.Select(c => c.Literals));
    }

    [Theory]
    [
        InlineData(nameof(SimpleOr), SimpleOr, 3),
        InlineData(nameof(TwoStateSudoku), TwoStateSudoku, 2),
        InlineData(nameof(ThreeStateSudoku), ThreeStateSudoku, 6),
        InlineData(nameof(FourStateSudoku), FourStateSudoku, 24)
    ]
    [Trait("Category", "Simple Cases")]
    public void EnumerateMutlipleSolutions(string name, string dimacs, int expectedSolutions)
    {
        var file = $"{name}.log";
        using var logger = DebugLogger.Log(file);
        var problem = DimacsParser.Parse(dimacs).Single();
        var solutions = problem.EnumerateSolutions(SatSolverOptions.Default).ToArray();
        SolutionValidator.Validate(problem, solutions);
        Assert.Equal(expectedSolutions, solutions.Length);
    }

    [Theory]
    [Trait("Category", "Simple Cases")]
    [MemberData(nameof(ProvideSimpleTestCases))]
    public void EnumerateSolutions_SimpleCases(string fileName) => SolveFile(Path.Combine("SimpleCases", fileName), SatSolverOptions.Default);
    [Theory]
    [Trait("Category", "SAT")]
    [MemberData(nameof(ProvideSatTestCases))]
    public void EnumerateSolutions_SAT(string fileName) => SolveFile(Path.Combine("SAT", fileName), true, SatSolverOptions.Default);
    [Theory]
    [Trait("Category", "UNSAT")]
    [MemberData(nameof(ProvideUnsatTestCases))]
    public void EnumerateSolutions_UNSAT(string fileName) => SolveFile(Path.Combine("UNSAT", fileName), false, SatSolverOptions.Default);

    static void SolveFile(string file, bool sat, SatSolverOptions options)
    {
        string cnf = File.ReadAllText(file);
        SolveCnf(Path.GetFileNameWithoutExtension(file), cnf, sat, options);
    }
    static void SolveFile(string file, SatSolverOptions options)
    {
        string cnf = File.ReadAllText(file);
        SolveCnf(Path.GetFileNameWithoutExtension(file), cnf, !cnf.Trim().EndsWith("c UNSAT"), options);
    }
    static void SolveCnf(string name, string cnf, bool sat, SatSolverOptions options)
    {
        var problem = DimacsParser.Parse(cnf).Single();

        using var logging = DebugLogger.Log($"{name}.log");

        var solutions = problem.EnumerateSolutions(options);
        if (sat)
            SolutionValidator.Validate(problem, solutions.First());
        else
            Assert.Empty(solutions);
    }

    public static TheoryData<string> ProvideSatTestCases() => ProvideTestCases("SAT");
    public static TheoryData<string> ProvideUnsatTestCases() => ProvideTestCases("UNSAT");
    public static TheoryData<string> ProvideSimpleTestCases() => ProvideTestCases("SimpleCases");
    static TheoryData<string> ProvideTestCases(string folder)
    {
        var data = new TheoryData<string>();
        data.AddRange([.. Directory.EnumerateFiles(folder).Select(file => Path.GetFileName(file))]);
        return data;
    }
}