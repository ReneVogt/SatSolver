using static Revo.SatSolver.BooleanAlgebra.RedundancyReducer;

namespace SatSolverTests.BooleanAlgebra;

public class RedundancyReducerTests
{
    [Fact]
    public void Reduce_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Reduce(null!));
    }
}
