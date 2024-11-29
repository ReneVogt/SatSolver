using Revo.SatSolver;

namespace SatSolverTests;

#pragma warning disable CA1861

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
            foreach (var (expectedClause, actualClause) in expectedProblem.Clauses.Zip(actualProblem.Clauses))
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

        yield return [@"
p cnf 5 5
1 0
2 0
3 0
4 0
5 0

c Comments and white spaces added

p cnf 4 3
c comment inside

-1 -2 4 0
-4 2 1 0
-3 -2 0

c End comment.
",
            new Problem[]
            {
                new(5, [
                    new[]{1},
                    new[]{2},
                    new[]{3},
                    new[]{4},
                    new[]{5}]),
                new(4, [
                    new[]{-1, -2, 4},
                    new[]{-4, 2, 1},
                    new[]{-3, -2}]),

            }
            ];
    }
    public static IEnumerable<object[]> FailingTestCases()
    {
        yield return ["", CnfParserException.Reason.InvalidProblemLine, 0, 0, "Missing or invalid problem definition at line 0."];
        yield return ["\n", CnfParserException.Reason.InvalidProblemLine, 1, 0, "Missing or invalid problem definition at line 1."];
        yield return ["p nocnf 2 5", CnfParserException.Reason.InvalidProblemFormat, 0, 2, "Invalid problem format 'nocnf' at line 0. Expected format 'cnf'."];
        yield return ["p cnf 3 5", CnfParserException.Reason.MissingClauses, 1, 0, "Missing 5 clause(s) at line 1."];
        yield return ["p cnf 0 5", CnfParserException.Reason.InvalidProblemLine, 0, 6, "Missing or invalid problem definition at line 0."];
        yield return ["p cnf 5 0", CnfParserException.Reason.InvalidProblemLine, 0, 8, "Missing or invalid problem definition at line 0."];

        yield return [@"p cnf 5 1
1 -1 0 x", CnfParserException.Reason.InvalidCharacter, 1, 7, "Invalid character at line 1, column 7: 'x'."];

        yield return [@"p cnf 5 3
1 -1 0
1 2 3 4 5 0
1 ab 2 3 4 0
", CnfParserException.Reason.InvalidCharacter, 3, 2, "Invalid character at line 3, column 2: 'ab'."];

        yield return [@"p cnf 5 3
1 -1 0
1 2 3 4 5 0
1 -5 2 3 4 0
c Achtung, baby!
p cnf 3 3
1 2 3 0
-1 -2 -3 0

c for completeness
p cnf 7 1
1 2 3 4 5 6 7 0
", CnfParserException.Reason.MissingClauses, 10, 0, "Missing 1 clause(s) at line 10."];

        yield return [@"p cnf 5 3
1 -1 0
1 2 3 4 5 0
1 -5 2 3 4 0
c Achtung, baby!
p cnf 3 3
1 2 3 0
-1 -2 -3
-3 2 1 0

c for completeness
p cnf 7 1
1 2 3 4 5 6 7 0
", CnfParserException.Reason.MissingTerminator, 7, 9, "Missing clause termination character '0' at line 7, column 9."];

        yield return [@"p cnf 5 3
1 -1 0
1 2 3 4 5 0
1 -5 2 3 4 0
c Achtung, baby!
p cnf 3 3
1 2 3 0
0
-3 2 1 0

c for completeness
p cnf 7 1
1 2 3 4 5 6 7 0
", CnfParserException.Reason.MissingLiteral, 7, 0, "Missing literal in line 7, column 0."];

        yield return [@"p cnf 5 3
1 -1 0
1 2 3 4 5 0
1 -5 2 3 4 0
c Achtung, baby!
p cnf 10 3
1 2 3 0
1 17 0
-3 2 1 0

c for completeness
p cnf 7 1
1 2 3 4 5 6 7 0
", CnfParserException.Reason.LiteralOutOfRange, 7, 4, "Literal '17' out of range (1 - 10) at line 7, column 4."];

        yield return [@"p cnf 5 3
1 -1 0
1 2 3 4 5 0
1 -5 2 3 4 0
c Achtung, baby!
p cnf 10 3
1 2 3 0
1 -17 0
-3 2 1 0

c for completeness
p cnf 7 1
1 2 3 4 5 6 7 0
", CnfParserException.Reason.LiteralOutOfRange, 7, 5, "Literal '17' out of range (1 - 10) at line 7, column 5."];
    }
}
