using Revo.SatSolver;
using Revo.SatSolver.Dimacs;

namespace SatSolverTests.Dimnacs;

public sealed class DimacsCnfParserTests
{
    [Fact]
    public void Parse_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DimacsCnfParser.Parse(null!));
    }

    [Theory]
    [MemberData(nameof(SuccessfulTestCases))]
    public void Parse_Success(string input, Problem[] problems)
    {
        var result = DimacsCnfParser.Parse(input);
        Assert.Equal(problems.Length, result.Length);
        foreach (var (expectedProblem, actualProblem) in problems.Zip(result))
        {
            Assert.Equal(expectedProblem.NumberOfLiterals, actualProblem.NumberOfLiterals);
            Assert.Equal(expectedProblem.NumberOfClauses, actualProblem.NumberOfClauses);
            foreach(var (expectedClause, actualClause) in expectedProblem.Clauses.Zip(actualProblem.Clauses))
            {
                Assert.Equal(expectedClause.Literals.Length, actualClause.Literals.Length);
                foreach (var (expectedLiteral, actualLiteral) in expectedClause.Literals.Zip(actualClause.Literals))
                {
                    Assert.Equal(expectedLiteral.Id, actualLiteral.Id);
                    Assert.Equal(expectedLiteral.Sense, actualLiteral.Sense);
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(FailingTestCases))]
    public void Parse_Exception(string input, CnfParserException.Reason reason, int line, int part, string message)
    {
        var exception = Assert.Throws<CnfParserException>(() => DimacsCnfParser.Parse(input).ToArray());
        Assert.Equal(reason, exception.Error);
        Assert.Equal(line, exception.Line);
        Assert.Equal(part, exception.Column);
        Assert.Equal(message, exception.Message);
    }

    public static IEnumerable<object[]> SuccessfulTestCases()
    {
        yield return [
            @"p cnf 1 1
1 0",
            new Problem[]
            {
                new(1, [new[]{1}])
            }
        ];
        yield return [
            @"p cnf 1 1
-1 0",
            new Problem[] 
            {
                new(1, [new[]{-1}]) 
            }
        ];
    }
    public static IEnumerable<object[]> FailingTestCases()
    {
        yield return ["", CnfParserException.Reason.InvalidProblemLine, 0, 0, "Missing or invalid problem definition at line 0."];
        yield return ["\n", CnfParserException.Reason.InvalidProblemLine, 1, 0, "Missing or invalid problem definition at line 1."];
        yield return ["p cnf 3 5", CnfParserException.Reason.MissingClauses, 1, 0, "Missing 5 clause(s) at line 1."];             
    }
}
