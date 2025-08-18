using Revo.SatSolver;
using Revo.SatSolver.Parsing;
using Xunit.Abstractions;
using static Revo.SatSolver.SatSolver;
using static SatSolverTests.Problems;

namespace SatSolverTests;

public sealed partial class SatSolverTests(ITestOutputHelper _output)
{
    [Fact]
    public void EnumerateSolutions_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EnumerateSolutions(null!));
    }
    [Fact]
    public void EnumerateSolutions_NoLiterals_EmptySolution()
    {
        var solution = EnumerateSolutions(new(0, [])).Single();
        Assert.NotNull(solution);
        Assert.Empty(solution);
    }
    [Fact]
    public void EnumerateSolutions_EmptyClause_NoSolution()
    {
        var solutions = EnumerateSolutions(new(2, [new([1, 2]), new([1]), new([]), new([2])]));
        Assert.Empty(solutions);
    }

    [Theory]
    [
        InlineData(SimpleOr, 3, false),
        InlineData(SimpleOr, 3, true),
        InlineData(TwoStateSudoku, 2, false),
        InlineData(TwoStateSudoku, 2, true),
        InlineData(ThreeStateSudoku, 6, false),
        InlineData(ThreeStateSudoku, 6, true),
        InlineData(FourStateSudoku, 24, false),
        InlineData(FourStateSudoku, 24, true)        
    ]
    public void EnumerateMutlipleSolutions(string dimacs, int expectedSolutions, bool cdcl)
    {
        using var logger = DebugLogger.Log(_output);
        var problem = DimacsParser.Parse(dimacs).Single();
        var solutions = EnumerateSolutions(problem, cdcl ? SatSolverOptions.CDCL : SatSolverOptions.PoorMansVSIDS).ToArray();
        Assert.Equal(expectedSolutions, solutions.Length);
        SolutionValidator.Validate(problem, solutions);
    }

    [Theory]
    [Trait("Category", "Simple Cases")]
    [Trait("Options", "Poor Man's VSIDS")]
    [MemberData(nameof(ProvideSimpleTestCases))]
    public void EnumerateSolutions_SimpleCases_PoorMansVSIDS(string fileName) => SolveFile(Path.Combine("SimpleCases", fileName), SatSolverOptions.PoorMansVSIDS);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "Poor Man's VSIDS")]
    [MemberData(nameof(ProvideSatTestCases))]
    public void EnumerateSolutions_SAT_PoorMansVSIDS(string fileName) => SolveFile(Path.Combine("SAT", fileName), true, SatSolverOptions.PoorMansVSIDS);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "Poor Man's VSIDS")]
    [MemberData(nameof(ProvideUnsatTestCases))]
    public void EnumerateSolutions_UNSAT_PoorMansVSIDS(string fileName) => SolveFile(Path.Combine("UNSAT", fileName), false, SatSolverOptions.PoorMansVSIDS);

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
        
        var solutions = EnumerateSolutions(problem, options);
        if (sat)
        {
            Assert.NotEmpty(solutions);
            SolutionValidator.Validate(problem, solutions.First());
        }
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