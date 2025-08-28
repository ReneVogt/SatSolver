using Revo.SatSolver.DataStructures;

namespace SatSolverTests.DataStructures;
public sealed class EmaTests
{
    [Theory]
    [MemberData(nameof(ProvideTestData))]
    public void Ema_DataDrivenTests(double halflife, double[] inputs, double[] expectedOutputs)
    {
        var sut = new Ema(halflife);
        foreach(var (input, expectedOutput) in inputs.Zip(expectedOutputs, (i, o) => (i, o)))
        {
            sut.Push(input);
            Assert.Equal(expectedOutput, sut.Value, 0.01d);
        }
    }

    public static TheoryData<double, double[], double[]> ProvideTestData() => new TheoryData<double, double[], double[]>
    {
        { 1, [1, 2, 3, 4], [1, 1.5, 2.25, 3.125] },
        { 2, [10, 10, 20, 20, 20, 20], [10, 10, 12.93, 15, 16.46, 17.5] }
    };
    
}
