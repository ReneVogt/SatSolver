using Revo.SatSolver;
using Revo.SatSolver.Parsing;
using Xunit.Abstractions;
using static Revo.SatSolver.SatSolver;

namespace SatSolverTests;

public sealed partial class SatSolverTests(ITestOutputHelper _output)
{
    [Fact]
    public void Solve_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Solve(null!));
    }
    [Fact]
    public void Solve_NoLiterals_EmptySolution()
    {
        var solution = Solve(new(0, []));
        Assert.NotNull(solution);
        Assert.Empty(solution);
    }
    [Fact]
    public void Solve_EmptyClause_Null()
    {
        var solution = Solve(new(2, [new([1, 2]), new([1]), new([]), new([2])]));
        Assert.Null(solution);
    }

    [Theory]
    [Trait("Category", "Simple Cases")]
    [Trait("Options", "Poor Man's VSIDS")]
    [MemberData(nameof(ProvideSimpleTestCases))]
    public void Solve_SimpleCases_PoorMansVSIDS(string fileName) => SolveFile(Path.Combine("SimpleCases", fileName), Options.PoorMansVSIDS);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "Poor Man's VSIDS")]
    [MemberData(nameof(ProvideSatTestCases))]
    public void Solve_SAT_PoorMansVSIDS(string fileName) => SolveFile(Path.Combine("SAT", fileName), true, Options.PoorMansVSIDS);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "Poor Man's VSIDS")]
    [MemberData(nameof(ProvideUnsatTestCases))]
    public void Solve_UNSAT_PoorMansVSIDS(string fileName) => SolveFile(Path.Combine("UNSAT", fileName), false, Options.PoorMansVSIDS);

    [Theory]
    [Trait("Category", "Simple Cases")]
    [Trait("Options", "CDCL")]
    [MemberData(nameof(ProvideSimpleTestCases))]
    public void Solve_SimpleCases_CDCL(string fileName) => SolveFile(Path.Combine("SimpleCases", fileName), Options.CDCL);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "CDCL")]
    [MemberData(nameof(ProvideSatTestCases))]
    public void Solve_SAT_CDCL(string fileName) => SolveFile(Path.Combine("SAT", fileName), true, Options.CDCL);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "CDCL")]
    [MemberData(nameof(ProvideUnsatTestCases))]
    public void Solve_UNSAT_CDCL(string fileName) => SolveFile(Path.Combine("UNSAT", fileName), false, Options.CDCL);

    void SolveFile(string file, Options options)
    {
        _output?.WriteLine(file);
        _output?.WriteLine(options.ToString());
        string cnf = File.ReadAllText(file);
        SolveCnf(cnf, !cnf.Trim().EndsWith("c UNSAT"), options);
    }
    void SolveFile(string file, bool sat, Options options)
    {
        _output?.WriteLine(file);
        _output?.WriteLine(options.ToString());
        string cnf = File.ReadAllText(file);
        SolveCnf(cnf, sat, options);
    }
    void SolveCnf(string cnf, bool sat, Options options)
    {
        var problem = DimacsParser.Parse(cnf).Single();
        
        using var logging = DebugLogger.Log(_output);
        
        var solution = Solve(problem, options);
        if (sat)
        {
            Assert.NotNull(solution);
            SolutionValidator.Validate(problem, solution);
        }
        else
            Assert.Null(Solve(problem, options));
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