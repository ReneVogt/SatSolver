using Revo.SatSolver;
using TestHelpers;
using static Revo.SatSolver.SatSolverFactory;

namespace SatSolverTests.ComponentTests.Solving;

[Trait("TestLevel", "Component")]
public sealed class IncrementalClauseInsertionTests
{
    [Fact]
    public void AddClause_SatisfiedClauseThenBackjump_FindsRemainingUnit()
    {
        var problem = new Problem(3, [new([-1])]);
        var solver = Create(problem);
        var initialSolution = solver.FindSolution();
        Assert.NotNull(initialSolution);
        SolutionValidator.Validate(problem, initialSolution);

        // Issue #4: the first insertion is satisfied by -3 in the initial model.
        solver.AddClause(new Clause([1, 2, -3]));
        solver.AddClause(new Clause([1, 3]));

        var accumulatedProblem = new Problem(3, [new([-1]), new([1, 2, -3]), new([1, 3])]);
        var solution = solver.FindSolution();
        Assert.NotNull(solution);
        Assert.Equal([-1, 2, 3], solution);
        SolutionValidator.Validate(accumulatedProblem, solution);
        Assert.Equal(Create(accumulatedProblem).FindSolution(), solution);
    }

    [Theory]
    [MemberData(nameof(ProvideInsertionSequences))]
    public void AddClause_Sequences_AgreeWithFreshSolverAndTruthTable(Clause[] additions, bool solveBetweenAdditions)
    {
        var clauses = new List<Clause> { new([-1]) };
        var solver = Create(new Problem(3, clauses));
        AssertEquivalent(solver, new Problem(3, clauses));

        foreach (var clause in additions)
        {
            solver.AddClause(clause);
            clauses.Add(clause);
            if (solveBetweenAdditions)
                AssertEquivalent(solver, new Problem(3, clauses));
        }

        var accumulatedProblem = new Problem(3, clauses);
        AssertEquivalent(solver, accumulatedProblem);
        solver.Reset(false);
        AssertEquivalent(solver, accumulatedProblem);
    }

    static void AssertEquivalent(ISatSolver solver, Problem problem)
    {
        // An independent oracle for these small formulas (only eight assignments).
        var satisfiable = Enumerable.Range(0, 1 << problem.NumberOfLiterals).Any(assignment =>
            problem.Clauses.All(clause => clause.Literals.Any(literal =>
                ((assignment & (1 << (literal.Id-1))) != 0) == literal.Sense)));

        var solution = solver.FindSolution();
        var freshSolution = Create(problem).FindSolution();
        Assert.Equal(satisfiable, solution is not null);
        Assert.Equal(satisfiable, freshSolution is not null);
        if (solution is not null) SolutionValidator.Validate(problem, solution);
        if (freshSolution is not null) SolutionValidator.Validate(problem, freshSolution);
    }

    public static TheoryData<Clause[], bool> ProvideInsertionSequences()
    {
        Clause[][] sequences =
        [
            [new([1, 2, -3]), new([1, 3])],
            [new([1, 2, 3]), new([-2]), new([-3])],
            [new([2, 3]), new([2]), new([1, -2, 3])],
            [new([-3]), new([1, 2, 3]), new([2, -3])]
        ];
        var data = new TheoryData<Clause[], bool>();
        foreach (var sequence in sequences)
        {
            data.Add(sequence, false);
            data.Add(sequence, true);
        }
        return data;
    }
}
