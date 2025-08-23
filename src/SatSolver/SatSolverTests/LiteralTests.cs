using Revo.SatSolver;

namespace SatSolverTests;

public sealed class LiteralTests
{
    [Fact]
    public void Literal_InvalidId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Literal(0, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Literal(-1, false));
    }

    [Fact]
    public void Literal_Positive_CorrectValues()
    {
        var sut = new Literal(12, true);
        Assert.Equal(12, sut.Id);
        Assert.True(sut.Sense);
        Assert.Equal(12, sut.GetHashCode());
        Assert.Equal("12", sut.ToString());
    }
    [Fact]
    public void Literal_Negative_CorrectValues()
    {
        var sut = new Literal(12, false);
        Assert.Equal(12, sut.Id);
        Assert.False(sut.Sense);
        Assert.Equal(-12, sut.GetHashCode());
        Assert.Equal("-12", sut.ToString());
    }
    [Fact]
    public void LiteralByCast_Positive_CorrectValues()
    {
        Literal sut = 12;
        Assert.Equal(12, sut.Id);
        Assert.True(sut.Sense);
        Assert.Equal(12, sut.GetHashCode());
        Assert.Equal("12", sut.ToString());
    }
    [Fact]
    public void LiteralByCast_Negative_CorrectValues()
    {
        var sut = (Literal)(-12);
        Assert.Equal(12, sut.Id);
        Assert.False(sut.Sense);
        Assert.Equal(-12, sut.GetHashCode());
        Assert.Equal("-12", sut.ToString());
    }

    [Fact]
    public void ToString_CorrectRepresentation()
    {
        var sut1 = new Literal(17, true);
        Assert.Equal("17", sut1.ToString());
        var sut2 = new Literal(23, false);
        Assert.Equal("-23", sut2.ToString());
    }
    [Fact]
    public void GetHashCode_CorrectValues()
    {
        var sut1 = new Literal(17, true);
        Assert.Equal(17, sut1.GetHashCode());
        var sut2 = new Literal(23, false);
        Assert.Equal(-23, sut2.GetHashCode());
    }
}
