using Revo.SatSolver;
using TestHelpers;
using static Revo.SatSolver.SatSolverFactory;

namespace SatSolverTests.ComponentTests.Solving;

[Trait("TestLevel", "Component")]
public sealed class UnsatisfiableStateTests
{
    [Theory]
    [MemberData(nameof(ProvideUnsatisfiableProblems))]
    public void FindSolution_Unsatisfiable_RepeatedCallsReturnNull(Problem problem)
    {
        var solver = Create(problem);

        Assert.Null(solver.FindSolution());
        Assert.Null(solver.FindSolution());
        Assert.Null(solver.FindSolution());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(2)]
    public void AddClause_AfterUnsatisfiable_RemainsUnsatisfiable(int literal)
    {
        var problem = new Problem(2, [new([1]), new([-1])]);
        var solver = Create(problem);
        Assert.Null(solver.FindSolution());

        var clause = new Clause([literal]);
        solver.AddClause(clause);

        Assert.Null(solver.FindSolution());
        Assert.Null(solver.FindSolution());
        var accumulatedProblem = new Problem(2, [.. problem.Clauses, clause]);
        Assert.Null(Create(accumulatedProblem).FindSolution());
    }

    [Fact]
    public void AddClause_AfterUnsatisfiable_StillValidatesArguments()
    {
        var solver = Create(new Problem(1, [new([1]), new([-1])]));
        Assert.Null(solver.FindSolution());

        Assert.Throws<ArgumentNullException>(() => solver.AddClause(null!));
        Assert.Throws<ArgumentException>(() => solver.AddClause(new Clause([])));
        Assert.Throws<ArgumentException>(() => solver.AddClause(new Clause([2])));
        Assert.Null(solver.FindSolution());
    }

    [Theory]
    [MemberData(nameof(ProvideUnsatisfiableProblems))]
    public void Reset_KeepingClauses_RemainsUnsatisfiable(Problem problem)
    {
        var solver = Create(problem);
        Assert.Null(solver.FindSolution());

        solver.Reset(false);

        Assert.Null(solver.FindSolution());
        Assert.Null(solver.FindSolution());
    }

    [Theory]
    [MemberData(nameof(ProvideUnsatisfiableProblems))]
    public void Reset_RemovingAdditionalClauses_OriginallyUnsatisfiableRemainsUnsatisfiable(Problem problem)
    {
        var solver = Create(problem);
        Assert.Null(solver.FindSolution());

        solver.Reset(true);

        Assert.Null(solver.FindSolution());
        Assert.Null(solver.FindSolution());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reset_RemovingConflictingAdditionalUnit_RecoversOriginalSolution(bool solveBeforeAdding)
    {
        var problem = new Problem(1, [new([1])]);
        var solver = Create(problem);
        if (solveBeforeAdding)
        {
            var initialSolution = solver.FindSolution();
            Assert.NotNull(initialSolution);
            Assert.Equal([1], initialSolution);
        }

        // Covers both pending propagation and a clause falsified by a root assignment.
        solver.AddClause(new Clause([-1]));
        Assert.Null(solver.FindSolution());
        Assert.Null(solver.FindSolution());

        solver.Reset(false);
        Assert.Null(solver.FindSolution());

        solver.Reset(true);
        var solution = solver.FindSolution();
        Assert.NotNull(solution);
        Assert.Equal([1], solution);
        Assert.Equal(Create(problem).FindSolution(), solver.FindSolution());

        // Removing clauses must also allow a later transition back to UNSAT.
        solver.AddClause(new Clause([-1]));
        Assert.Null(solver.FindSolution());
        Assert.Null(solver.FindSolution());
    }

    [Fact]
    public void Reset_RemovingAdditionalAndLearnedClauses_RecoversOriginalSolution()
    {
        var problem = new Problem(2, [new([1, 2]), new([1, -2]), new([-1, 2])]);
        var solver = Create(problem);

        // All four binary clauses together exclude every assignment. With no initial
        // units, proving UNSAT requires a decision and conflict learning.
        var clause = new Clause([-1, -2]);
        solver.AddClause(clause);
        Assert.Null(solver.FindSolution());
        Assert.Null(solver.FindSolution());
        Assert.Null(Create(new Problem(2, [.. problem.Clauses, clause])).FindSolution());

        solver.Reset(false);
        Assert.Null(solver.FindSolution());

        solver.Reset(true);
        var solution = solver.FindSolution();
        Assert.NotNull(solution);
        SolutionValidator.Validate(problem, solution);
        Assert.Equal(Create(problem).FindSolution(), solution);
        Assert.Equal(solution, solver.FindSolution());
    }

    [Fact]
    public void FindSolution_UnsatisfiableAndCanceled_ThrowsOperationCanceledException()
    {
        var solver = Create(new Problem(1, [new([1]), new([-1])]));
        Assert.Null(solver.FindSolution());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => solver.FindSolution(cts.Token));
        Assert.Null(solver.FindSolution());
    }

    public static TheoryData<Problem> ProvideUnsatisfiableProblems() =>
    [
        new Problem(1, [new([1]), new([-1])]),
        new Problem(2, [new([1, 2]), new([1, -2]), new([-1, 2]), new([-1, -2])])
    ];
}
