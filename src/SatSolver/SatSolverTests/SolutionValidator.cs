using Revo.SatSolver;

namespace SatSolverTests;

static class SolutionValidator
{
    public static void Validate(Problem problem, Literal[] solution)
    {
        var variables = solution.ToDictionary(literal => literal.Id, literal => literal.Sense);
        if (problem.Clauses.Any(clause => clause.Literals.All(literal => variables[literal.Id] != literal.Sense)))
            Assert.Fail("Solution does not work.");
    }
    public static void Validate(Problem problem, IEnumerable<Literal[]> solutions) 
    {
        foreach(var solution in solutions) Validate(problem, solution);
    }
}
