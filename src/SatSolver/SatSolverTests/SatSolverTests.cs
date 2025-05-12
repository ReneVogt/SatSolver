using Revo.SatSolver.Parsing;
using Xunit.Abstractions;
using static Revo.SatSolver.SatSolver;

namespace SatSolverTests;

public sealed partial class SatSolverTests(ITestOutputHelper _output)
{
    static readonly Options _testOptions = new()
    {
        OnlyPoorMansVSIDS = true,

        VariableActivityDecayFactor = 0.9995,

        ClauseActivityDecayFactor = 0.999,
        LiteralBlockDistanceMaximum = 8,

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
        var solution = Solve(new(0, []), _testOptions);
        Assert.NotNull(solution);
        Assert.Empty(solution);
    }
    [Fact]
    public void Solve_EmptyClause_Null()
    {
        var solution = Solve(new(2, [new([1, 2]), new([1]), new([]), new([2])]), _testOptions);
        Assert.Null(solution);
    }

    [Theory]
    [MemberData(nameof(ProvideSimpleTestCases))]
    public void Solve_SimpleCases(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("SimpleCases", fileName));
        var problem = DimacsParser.Parse(cnf).Single();
        var solution = Solve(problem, _testOptions);
        Assert.NotNull(solution);
        SolutionValidator.Validate(problem, solution);
    }

    [Theory]
    [Trait("Category", "Benchmark")]
    [MemberData(nameof(ProvideSatTestCases))]
    public void Solve_SAT(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("SAT", fileName));
        var problem = DimacsParser.Parse(cnf).Single();
        _output?.WriteLine($"{fileName} Literals: {problem.NumberOfLiterals} Clauses: {problem.Clauses.Length}");
        var solution = Solve(problem, _testOptions);
        Assert.NotNull(solution);
        SolutionValidator.Validate(problem, solution);
    }

    [Theory]
    [Trait("Category", "Benchmark")]
    [MemberData(nameof(ProvideUnsatTestCases))]
    public void Solve_UNSAT(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("UNSAT", fileName));
        var problem = DimacsParser.Parse(cnf).Single();
        _output?.WriteLine($"{fileName} Literals: {problem.NumberOfLiterals} Clauses: {problem.Clauses.Length}");
        Assert.Null(Solve(problem, _testOptions));
    }

    public static TheoryData<string> ProvideSatTestCases()
    {
        var data = new TheoryData<string>();
        data.AddRange([.. Directory.EnumerateFiles("SAT").Select(file => Path.GetFileName(file))]);
        return data;
    }
    public static TheoryData<string> ProvideUnsatTestCases()
    {
        var data = new TheoryData<string>();
        data.AddRange([.. Directory.EnumerateFiles("UNSAT").Select(file => Path.GetFileName(file))]);
        return data;
    }

    public static TheoryData<string> ProvideSimpleTestCases()
    {
        var data = new TheoryData<string>();
        data.AddRange([.. Directory.EnumerateFiles("SimpleCases").Select(file => Path.GetFileName(file))]);
        return data;
    }
}