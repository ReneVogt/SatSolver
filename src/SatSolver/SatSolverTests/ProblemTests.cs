using Revo.SatSolver;
using Revo.SatSolver.Parsing;

namespace SatSolverTests;

public sealed class ProblemTests
{
    [Fact]
    public void Constructor_NullClauses_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Problem(10, null!));
    }
    [Fact]
    public void Constructor_NegativeClausesCount_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Problem(-1, []));
    }
    [Fact]
    public void Constructor_InvalidLiteral_Throws()
    {
        var clause1 = new Clause([1, 2]);
        var clause2 = new Clause([-1, 3]);
        var clause3 = new Clause([2, -3]);
        Assert.Throws<ArgumentException>(() => new Problem(2, [clause1, clause2, clause3]));
    }

    [Fact]
    public void Constructor_CorrectlyInitialized()
    {
        var clause1 = new Clause([1, 2]);
        var clause2 = new Clause([-1, 3]);
        var clause3 = new Clause([2, -3]);
        var clause4 = new Clause([-1, 2, -3]);
        var sut = new Problem(3, [clause1, clause2, clause3, clause4]);

        Assert.Equal(3, sut.NumberOfLiterals);
        Assert.Equal(4, sut.NumberOfClauses);
        Assert.Equal([clause2, clause1, clause3, clause4], sut.Clauses.AsEnumerable());
    }
    [Fact]
    public void ToString_CorrectRepresentation()
    {
        var clause1 = new Clause([1, 2]);
        var clause2 = new Clause([-1, 3]);
        var clause3 = new Clause([2, -3]);
        var clause4 = new Clause([-1, 2, -3]);
        var sut = new Problem(3, [clause1, clause2, clause3, clause4]);

        Assert.Equal(@"p cnf 3 4
-1 3 0
1 2 0
2 -3 0
-1 2 -3 0", sut.ToString());
    }
    [Fact]
    public void Equals_Object()
    {
        var clause1 = new Clause([1, 2]);
        var clause2 = new Clause([-1, 3]);
        var clause3 = new Clause([2, -3]);
        var clause4 = new Clause([-1, 2, -3]);
        var sut = new Problem(3, [clause1, clause2, clause3, clause4]);
        Assert.False(sut.Equals(new object()));
        Assert.False(sut.Equals((object)null!));
        Assert.False(sut.Equals((Problem)null!));
        Assert.True(sut.Equals((object)sut));
    }
    [Theory]
    [MemberData(nameof(ProvideEqualsTestCases))]
    public void Equals_CorrectComparsion(Problem problem1, Problem problem2, bool equal) => Assert.Equal(equal, problem1.Equals(problem2));

    public static TheoryData<Problem, Problem, bool> ProvideEqualsTestCases()
    {
        var problem1 = DimacsParser.Parse(@"p cnf 3 3
-1 2 3 0
2 -3 0
1 -2 0").Single();
        var problem2 = DimacsParser.Parse(@"p cnf 3 3
1 -2 0
2 -3 0
-1 2 3 0").Single();
        var problem3 = DimacsParser.Parse(@"p cnf 3 4
1 -2 0
2 -3 0
-1 2 3 0
1 3 0").Single();
        var problem4 = DimacsParser.Parse(@"p cnf 2 3
1 -2 0
2 -1 0
-1 2 0").Single();

        var data = new TheoryData<Problem, Problem, bool>
        {
            { problem1, problem1, true },
            { problem1, problem2, true },
            { problem1, problem3, false },
            { problem1, problem4, false }
        };

        return data;
    }
}
