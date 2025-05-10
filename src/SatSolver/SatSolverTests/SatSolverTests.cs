using Revo.SatSolver.Parsing;
using Xunit.Abstractions;
using static Revo.SatSolver.SatSolver;

namespace SatSolverTests;

public sealed partial class SatSolverTests(ITestOutputHelper _output)
{
    static readonly Options _testOptions = new()
    {
        VariableActivityDecayFactor = 0.95d,
        ClauseActivityDecayFactor = 0.99d,
        LiteralBlockDistanceLimit = 5,
        LiteralBlockDistanceToKeep = 2,
        ClauseDeletionInterval = 3000,
        ClauseDeletionRatio = 0.5,

        RestartMode = RestartMode.MeanLBD,
        RestartInterval = 0,
        LiteralBlockDistanceDecay = 0.999,
        LiteralBlockDistanceQueueSize = 200,
        RestartLiteralBlockDistanceThreshold = 2
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