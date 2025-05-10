using Revo.SatSolver;

namespace SatSolverTests;

public sealed class LubySequenceTests
{
    [Fact]
    public void Enumerate_Base1()
    {
        var expected = new long[] { 1, 1, 2, 1, 1, 2, 4, 1, 1, 2, 1, 1, 2, 4, 8, 1, 1, 2 };
        Assert.Equal(expected, LubySequence.Enumerate().Take(expected.Length));
    }
    [Fact]
    public void Enumerate_Base100()
    {
        var expected = new long[] { 100, 100, 200, 100, 100, 200, 400, 100, 100, 200, 100, 100, 200, 400, 800, 100, 100, 200};
        Assert.Equal(expected, LubySequence.Enumerate(100).Take(expected.Length));
    }

    const int TESTSIZE = 100000;

    [Fact]
    public void Enumerate_Base1_CompareToRecursive()
    {
        Assert.Equal(Enumerable.Range(1, TESTSIZE).Select(n => RecursiveLuby(n)), LubySequence.Enumerate().Take(TESTSIZE));
    }

    [Fact]
    public void Enumerate_Base50_CompareToRecursive()
    {
        Assert.Equal(Enumerable.Range(1, TESTSIZE).Select(n => 50 * RecursiveLuby(n)), LubySequence.Enumerate(50).Take(TESTSIZE));
    }

    public static long RecursiveLuby(int i)
    {
        var k = (int)Math.Log2(i + 1);
        if (((i + 1) & i) == 0)
            return 1 << (k - 1);

        return RecursiveLuby(i + 1 - (1 << k));
    }
}
