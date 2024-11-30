using Revo.SatSolver;
using Revo.SatSolver.Parsing;

namespace SatSolverTests.Parsing;

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
    public void Parse_Exception(string input, CnfParserException.Reason reason, int line, int position, string message)
    {
        var exception = Assert.Throws<CnfParserException>(() => DimacsCnfParser.Parse(input).ToArray());
        Assert.Equal(reason, exception.Error);
        Assert.Equal(line, exception.Line);
        Assert.Equal(position, exception.Position);
        Assert.Equal(message, exception.Message);
    }

    public static IEnumerable<object[]> SuccessfulTestCases()
    {
        yield return [
            @"c start with comments

c and blank lines

p cnf 1 1
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
        yield return [@"
p  cnf  5 5
1 0
2 0
  3 0
4  0 
5 0 test for non strictness

c Comments and white spaces added
%
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
                    new[]{5}])
            }];
    }
    public static IEnumerable<object[]> FailingTestCases()
    {
        yield return ["", CnfParserException.Reason.InvalidProblemLine, 0, 0, "Missing or invalid problem definition in line 0."];
        yield return ["\n", CnfParserException.Reason.InvalidProblemLine, 1, 0, "Missing or invalid problem definition in line 1."];
        yield return ["p  nocnf 2 5", CnfParserException.Reason.InvalidProblemFormat, 0, 3, "Invalid problem format 'nocnf' in line 0, position 3. Expected format 'cnf'."];
        yield return ["p cnf 3 5", CnfParserException.Reason.MissingLiteral, 0, 9, "Missing literal or clause termination ('0') for clause 1 of 5 in line 0, position 9."];
        yield return ["p cnf 0 5", CnfParserException.Reason.InvalidProblemLine, 0, 6, "Missing or invalid problem definition in line 0."];
        yield return ["p cnf 5 0", CnfParserException.Reason.InvalidProblemLine, 0, 8, "Missing or invalid problem definition in line 0."];
        yield return [@"

% 
p cnf 5 1
1 2 3 0
", CnfParserException.Reason.InvalidProblemLine, 2, 0, "Missing or invalid problem definition in line 2."];

        yield return [@"p cnf 5 3
1 -1 0
1 2 3 4 5 0
1 ab 2 3 4 0
", CnfParserException.Reason.MissingLiteral, 3, 2, "Missing literal or clause termination ('0') for clause 3 of 3 in line 3, position 2."];

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
", CnfParserException.Reason.MissingLiteral, 10, 0, "Missing literal or clause termination ('0') for clause 3 of 3 in line 10, position 0."];

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
", CnfParserException.Reason.MissingLiteral, 7, 9, "Missing literal or clause termination ('0') for clause 2 of 3 in line 7, position 9."];

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
", CnfParserException.Reason.MissingLiteral, 7, 0, "Missing literal or clause termination ('0') for clause 2 of 3 in line 7, position 0."];

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
", CnfParserException.Reason.LiteralOutOfRange, 7, 2, "Literal '17' out of range (1 - 10) in line 7, position 2."];

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
", CnfParserException.Reason.LiteralOutOfRange, 7, 2, "Literal '17' out of range (1 - 10) in line 7, position 2."];

        yield return [@"p cnf 5 3
1 -1 0
1 2 3 4 5 0
 %
4 -2 0
", CnfParserException.Reason.MissingLiteral, 3, 1, "Missing literal or clause termination ('0') for clause 3 of 3 in line 3, position 1."];

    }

}
