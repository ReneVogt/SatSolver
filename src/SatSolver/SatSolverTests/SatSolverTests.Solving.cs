using Revo.SatSolver;
using Revo.SatSolver.Parsing;
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
    [Trait("Options", "CDCL")]
    public void EnumerateSolutions_NoClauses_AllSolutions_CDCL()
    {
        var solutions = new Problem(3, []).EnumerateSolutions(SatSolverOptions.CDCL).ToArray();
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
    [Fact]
    [Trait("Category", "Simple Cases")]
    [Trait("Options", "DPLL")]
    public void EnumerateSolutions_NoClauses_AllSolutions_DPLL()
    {
        var solutions = new Problem(3, []).EnumerateSolutions(SatSolverOptions.DPLL).ToArray();
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
        InlineData(SimpleOr, 3),
        InlineData(TwoStateSudoku, 2),
        InlineData(ThreeStateSudoku, 6),
        InlineData(FourStateSudoku, 24),
    ]
    [Trait("Category", "Simple Cases")]
    [Trait("Options", "CDCL")]
    public void EnumerateMutlipleSolutions_CDCL(string dimacs, int expectedSolutions) => EnumerateMutlipleSolutions(dimacs, expectedSolutions, true);
    [Theory]
    [
        InlineData(SimpleOr, 3),
        InlineData(TwoStateSudoku, 2),
        InlineData(ThreeStateSudoku, 6),
        InlineData(FourStateSudoku, 24),
    ]
    [Trait("Category", "Simple Cases")]
    [Trait("Options", "DPLL")]
    public void EnumerateMutlipleSolutions_DPLL(string dimacs, int expectedSolutions) => EnumerateMutlipleSolutions(dimacs, expectedSolutions, false);
    void EnumerateMutlipleSolutions(string dimacs, int expectedSolutions, bool cdcl)
    {
        using var logger = DebugLogger.Log(_output);
        var problem = DimacsParser.Parse(dimacs).Single();
        var solutions = problem.EnumerateSolutions(cdcl ? SatSolverOptions.CDCL : SatSolverOptions.DPLL).ToArray();
        SolutionValidator.Validate(problem, solutions);
        Assert.Equal(expectedSolutions, solutions.Length);
    }

    [Theory]
    [Trait("Category", "Simple Cases")]
    [Trait("Options", "DPLL")]
    [MemberData(nameof(ProvideSimpleTestCases))]
    public void EnumerateSolutions_SimpleCases_DPLL(string fileName) => SolveFile(Path.Combine("SimpleCases", fileName), SatSolverOptions.DPLL);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "DPLL")]
    [MemberData(nameof(ProvideSatTestCases))]
    public void EnumerateSolutions_SAT_DPLL(string fileName) => SolveFile(Path.Combine("SAT", fileName), true, SatSolverOptions.DPLL);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "DPLL")]
    [MemberData(nameof(ProvideUnsatTestCases))]
    public void EnumerateSolutions_UNSAT_DPLL(string fileName) => SolveFile(Path.Combine("UNSAT", fileName), false, SatSolverOptions.DPLL);

    [Theory]
    [Trait("Category", "Simple Cases")]
    [Trait("Options", "CDCL")]
    [MemberData(nameof(ProvideSimpleTestCases))]
    public void EnumerateSolutions_SimpleCases_CDCL(string fileName) => SolveFile(Path.Combine("SimpleCases", fileName), SatSolverOptions.CDCL);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "CDCL")]
    [MemberData(nameof(ProvideSatTestCases))]
    public void EnumerateSolutions_SAT_CDCL(string fileName) => SolveFile(Path.Combine("SAT", fileName), true, SatSolverOptions.CDCL);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "CDCL")]
    [MemberData(nameof(ProvideUnsatTestCases))]
    public void EnumerateSolutions_UNSAT_CDCL(string fileName) => SolveFile(Path.Combine("UNSAT", fileName), false, SatSolverOptions.CDCL);

    void SolveFile(string file, SatSolverOptions options)
    {
        _output?.WriteLine(file);
        _output?.WriteLine(options.ToString());
        string cnf = File.ReadAllText(file);
        SolveCnf(cnf, !cnf.Trim().EndsWith("c UNSAT"), options);
    }
    void SolveFile(string file, bool sat, SatSolverOptions options)
    {
        _output?.WriteLine(file);
        _output?.WriteLine(options.ToString());
        string cnf = File.ReadAllText(file);
        SolveCnf(cnf, sat, options);
    }
    void SolveCnf(string cnf, bool sat, SatSolverOptions options)
    {
        var problem = DimacsParser.Parse(cnf).Single();
        
        using var logging = DebugLogger.Log(_output);
        
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