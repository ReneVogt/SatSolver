using Revo.SatSolver;
using Revo.SatSolver.Parsing;
using Xunit.Abstractions;

namespace SatSolverTests;

public sealed partial class SatSolverTests(ITestOutputHelper _output)
{
    static readonly SatSolver.Options _testOptions = new()
    {
        ActivityDecayInterval = 1,
        ActivityDecayFactor = 0.95d,
        LiteralBlockDistanceLimit = 2, // increase when clause deletion will be added?
        RestartInterval = 0,
        LubyfyRestart = true
    };

    [Fact]
    public void Solve_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SatSolver.Solve(null!));
    }
    [Fact]
    public void Solve_NoLiterals_EmptySolution()
    {
        var solution = SatSolver.Solve(new(0, []), _testOptions);
        Assert.NotNull(solution);
        Assert.Empty(solution);
    }
    [Fact]
    public void Solve_EmptyClause_Null()
    {
        var solution = SatSolver.Solve(new(2, [new([1, 2]), new([1]), new([]), new([2])]), _testOptions);
        Assert.Null(solution);
    }

    [Theory]
    [MemberData(nameof(ProvideSimpleTestCases))]
    public void Solve_SimpleCases(string fileName)
    {
        string cnf = File.ReadAllText(Path.Combine("SimpleCases", fileName));
        var problem = DimacsParser.Parse(cnf).Single();
        var solution = SatSolver.Solve(problem, _testOptions);
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
        var solution = SatSolver.Solve(problem, _testOptions);
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
        Assert.Null(SatSolver.Solve(problem, _testOptions));
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