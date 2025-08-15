using Revo.SatSolver.Parsing;
using Xunit.Abstractions;
using static Revo.SatSolver.SatSolver;

namespace SatSolverTests;

public sealed partial class SatSolverTests(ITestOutputHelper _output)
{
    static readonly Options _poorMansVsidsOptions = new()
    {
        OnlyPoorMansVSIDS = true,

        VariableActivityDecayFactor = 0.9995,

        ClauseActivityDecayFactor = 0.999,
        MaximumLiteralBlockDistance = 8,
        MaximumClauseMinimizationDepth = 9,

        ClauseDeletion = new()
        {
            LiteralBlockDistanceToKeep = 2,
            OriginalClauseCountFactor = 5,
            RatioToDelete = 0.5,
            LiteralBlockDistanceThreshold = 1.3,
            PropagationRateThreshold = 0.5
        },

        Restart = new()
        {
            Interval = null,
            Luby = false,
            LiteralBlockDistanceThreshold = null,
            PropagationRateThreshold = null
        },

        LiteralBlockDistanceTracking = new()
        {
            Decay = 0.999,
            RecentCount = 100
        },

        PropagationRateTracking = new()
        {
            ConflictInterval = 500,
            Decay = 0.999,
            SampleSize = 50
        }
    };
    static readonly Options _cdclNoRestartOptions = new()
    {
        OnlyPoorMansVSIDS = false,

        VariableActivityDecayFactor = 0.95,

        ClauseActivityDecayFactor = 0.999,
        MaximumLiteralBlockDistance = 8,
        MaximumClauseMinimizationDepth = 9,

        ClauseDeletion = new()
        {
            LiteralBlockDistanceToKeep = 2,
            OriginalClauseCountFactor = 5,
            RatioToDelete = 0.5,
            LiteralBlockDistanceThreshold = 1.3,
            PropagationRateThreshold = 0.5
        },

        Restart = new()
        {
            Interval = null,
            Luby = false,
            LiteralBlockDistanceThreshold = null,
            PropagationRateThreshold = null
        },

        LiteralBlockDistanceTracking = new()
        {
            Decay = 0.999,
            RecentCount = 100
        },

        PropagationRateTracking = new()
        {
            ConflictInterval = 500,
            Decay = 0.999,
            SampleSize = 50
        }
    };

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
    public void Solve_SimpleCases_PoorMansVSIDS(string fileName) => SolveFile(Path.Combine("SimpleCases", fileName), _poorMansVsidsOptions);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "Poor Man's VSIDS")]
    [MemberData(nameof(ProvideSatTestCases))]
    public void Solve_SAT_PoorMansVSIDS(string fileName) => SolveFile(Path.Combine("SAT", fileName), true, _poorMansVsidsOptions);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "Poor Man's VSIDS")]
    [MemberData(nameof(ProvideUnsatTestCases))]
    public void Solve_UNSAT_PoorMansVSIDS(string fileName) => SolveFile(Path.Combine("UNSAT", fileName), false, _poorMansVsidsOptions);


    [Theory]
    [Trait("Category", "Simple Cases")]
    [Trait("Options", "CDCL no restart")]
    [MemberData(nameof(ProvideSimpleTestCases))]
    public void Solve_SimpleCases_CDCLNoRestart(string fileName) => SolveFile(Path.Combine("SimpleCases", fileName), _cdclNoRestartOptions);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "CDCL no restart")]
    [MemberData(nameof(ProvideSatTestCases))]
    public void Solve_SAT_CDCLNoRestart(string fileName) => SolveFile(Path.Combine("SAT", fileName), true, _cdclNoRestartOptions);
    [Theory]
    [Trait("Category", "Benchmark")]
    [Trait("Options", "CDCL no restart")]
    [MemberData(nameof(ProvideUnsatTestCases))]
    public void Solve_UNSAT_CDCLNoRestart(string fileName) => SolveFile(Path.Combine("UNSAT", fileName), false, _cdclNoRestartOptions);

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
    static void SolveCnf(string cnf, bool sat, Options options)
    {
        var problem = DimacsParser.Parse(cnf).Single();
        
        //using var logging = DebugLogger.Log(_output);
        
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