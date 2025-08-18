using Revo.SatSolver;
using Revo.SatSolver.Parsing;
using Xunit.Abstractions;
using static Revo.SatSolver.SatSolver;

namespace SatSolverTests;

public sealed partial class SatSolverInitializerTests(ITestOutputHelper _output)
{
    [Fact]
    public void ThreeStateSudoku_CorrectWatchers()
    {
        var problem = DimacsParser.Parse(Problems.ThreeStateSudoku).Single();
        var state = new SatSolverInitializer(problem, new(), default).Initialize();

        Assert.Equal("-1 -2 | -1 -3",
            string.Join(" | ",
                state.Literals[1].Watchers.Select(constraint => string.Join(" ", constraint.Literals.Select(literal => literal.Orientation ? literal.Variable.Index + 1 : -(literal.Variable.Index + 1))))));
    }
}