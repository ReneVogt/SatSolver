using Revo.SatSolver;

namespace TestHelpers;

public static class SolutionValidator
{
    public static void Validate(Problem problem, Literal[] solution)
    {
        var variables = solution.ToDictionary(literal => literal.Id, literal => literal.Sense);
        if (problem.Clauses.Any(clause => clause.Literals.Where(literal => variables.ContainsKey(literal.Id)).All(literal => variables[literal.Id] != literal.Sense)))
            throw new Exception("Solution does not work.");
    }
    public static void Validate(Problem problem, IEnumerable<Literal[]> solutions) 
    {
        foreach(var solution in solutions) Validate(problem, solution);
    }
}
