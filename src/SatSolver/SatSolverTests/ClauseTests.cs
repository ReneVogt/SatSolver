using Revo.SatSolver;

namespace SatSolverTests;

#pragma warning disable IDE0079
#pragma warning disable CA1861

public sealed class ClauseTests
{
    [Fact]
    public void Constructor_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Clause(null!));
    }
    [Fact]
    public void CorrectInitialization()
    {
        var literals = new Literal[] { 1, 10, -3, -5, 5, 10, 8 };
        var expected = new[] { 0, 2, 3, 4, 6, 1 }.Select(i => literals[i]);
        var sut = new Clause(literals);
        Assert.Equal(expected, sut.Literals);
    }
    [Fact]
    public void CastFromIntArray()
    {
        var literals = new [] { 1, 10, -3, -5, 5, 10, 8 };
        var expected = new[] { 0, 2, 3, 4, 6, 1 }.Select(i => (Literal)literals[i]);
        var sut = (Clause)literals;
        Assert.Equal(expected, sut.Literals);
    }
    [Fact]
    public void CastFromLiteralArray()
    {
        var literals = new Literal[] { 1, 10, -3, -5, 5, 10, 8 };
        var expected = new[] { 0, 2, 3, 4, 6, 1 }.Select(i => literals[i]);
        var sut = (Clause)literals;
        Assert.Equal(expected, sut.Literals);
    }
    [Fact]
    public void ToString_CorrectRepresentation()
    {
        var sut = new Clause([1, 10, -3, -5, 5, 10, 8]);
        Assert.Equal("1 -3 -5 5 8 10 0", sut.ToString());

        sut = new Clause([]);
        Assert.Equal("0", sut.ToString());
    }
    [Fact]
    public void CorrectComparison()
    {
        var sut1 = new Clause([1, -2, 3]);
        var sut1b = new Clause([1, -2, 3]);
        var sut2 = new Clause([1, -2, 3, 4]);
        var sut3 = new Clause([-1, -2, 3]);

        Assert.False(sut1.Equals(null));
        Assert.False(sut1.Equals((object)null!));

        Assert.True(sut1 == sut1b);
        Assert.Equal(sut2, sut2);
        Assert.True(sut3.Equals(sut3));
        Assert.True(sut3.Equals((object)sut3));
        Assert.False(sut1 != sut1b);
        Assert.Equal(0, sut1.CompareTo(sut1b));

        Assert.True(sut1 <= sut1b);
        Assert.True(sut1 >= sut1b);

        Assert.True(sut3 <= sut1);
        Assert.True(sut3 < sut1);

        Assert.True(sut1 <= sut2);
        Assert.True(sut1 < sut2);
        Assert.False(sut1 >= sut2);
        Assert.False(sut1 > sut2);
        Assert.True(sut1 != sut2);
        Assert.False(sut1 == sut3);

        Assert.False(sut1 == null!);
        Assert.True(sut1 >= null!);
        Assert.False(sut1 <= null!);
        Assert.False(sut1 < null!);
        Assert.True(sut1 > null!);
        Assert.True(sut1 != null!);

        Assert.False(null! == sut1);
        Assert.False(null! >= sut1);
        Assert.True(null! <= sut1);
        Assert.False(null! > sut1);
        Assert.True(null! < sut1);
        Assert.True(null! != sut1);
    }
}
