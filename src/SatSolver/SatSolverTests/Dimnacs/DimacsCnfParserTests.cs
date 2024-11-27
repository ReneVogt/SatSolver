using Revo.SatSolver.Dimacs;

namespace SatSolverTests.Dimnacs;

public sealed class DimacsCnfParserTests
{
    [Fact]
    public void Parse_Null_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DimacsCnfParser.Parse(null!));
    }
}
