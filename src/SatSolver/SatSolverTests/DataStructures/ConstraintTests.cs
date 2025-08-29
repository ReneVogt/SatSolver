using Revo.SatSolver.DataStructures;

namespace SatSolverTests.DataStructures;

public sealed class ConstraintTests
{
    [Fact]
    public void ToString_DimacsClauseNotation()
    {
        var v1 = new Variable(17);
        var v2 = new Variable(42);
        var v3 = new Variable(23);
        var sut = new Constraint([v1.PositiveLiteral, v2.NegativeLiteral, v3.NegativeLiteral], v1.PositiveLiteral, v2.NegativeLiteral);

        Assert.Equal("18 -43 -24", sut.ToString());
    }
}
