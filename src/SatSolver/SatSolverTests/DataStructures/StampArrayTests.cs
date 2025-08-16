using Revo.SatSolver.DataStructures;

namespace SatSolverTests.DataStructures;

public sealed class StampArrayTests
{
    [Fact]
    public void Add_SingleValue()
    {
        var sut = new StampArray();
        Assert.True(sut.Add(1000));
        Assert.Equal(1, sut.Count);
        Assert.True(sut.Contains(1000));
        Assert.False(sut.Contains(1001));
        Assert.False(sut.Add(1000));
        Assert.Equal(1, sut.Count);
    }

    [Fact]
    public void Add_MutlipleValues()
    {
        var sut = new StampArray();
        Assert.True(sut.Add(1000));
        Assert.Equal(1, sut.Count);
        Assert.True(sut.Contains(1000));
        Assert.False(sut.Contains(1001));
        
        Assert.True(sut.Add(1001));
        Assert.Equal(2, sut.Count);
        Assert.True(sut.Contains(1000));
        Assert.True(sut.Contains(1001));
    }

    [Fact]
    public void Add_Clear_Clears()
    {
        var sut = new StampArray();
        Assert.True(sut.Add(1000));
        Assert.Equal(1, sut.Count);
        Assert.True(sut.Contains(1000));
        Assert.False(sut.Contains(1001));

        Assert.True(sut.Add(1001));
        Assert.Equal(2, sut.Count);
        Assert.True(sut.Contains(1000));
        Assert.True(sut.Contains(1001));

        sut.Clear();
        Assert.Equal(0, sut.Count);
        Assert.False(sut.Contains(1000));
        Assert.False(sut.Contains(1001));

        Assert.True(sut.Add(1000));
        Assert.Equal(1, sut.Count);
        Assert.True(sut.Contains(1000));
        Assert.False(sut.Contains(1001));

        Assert.True(sut.Add(1001));
        Assert.Equal(2, sut.Count);
        Assert.True(sut.Contains(1000));
        Assert.True(sut.Contains(1001));
    }

    [Fact]
    public void EnumerateIndices_CorrectIndices()
    {
        var sut = new StampArray();
        Assert.True(sut.Add(17));
        Assert.True(sut.Add(18));
        Assert.True(sut.Add(20));
        Assert.False(sut.Add(18));
        Assert.Equal(3, sut.Count);

        Assert.Equal([17, 18, 20], sut.EnumerateIndices());

        Assert.True(sut.Remove(18));
        Assert.Equal([17, 20], sut.EnumerateIndices());

        sut.Clear();
        Assert.Empty(sut.EnumerateIndices());
    }

    [Fact]
    public void Remove()
    {
        var sut = new StampArray();
        Assert.True(sut.Add(17));
        Assert.True(sut.Add(18));
        Assert.True(sut.Add(20));
        Assert.False(sut.Add(18));
        Assert.Equal(3, sut.Count);

        Assert.Equal([17, 18, 20], sut.EnumerateIndices());

        Assert.True(sut.Remove(18));
        Assert.False(sut.Remove(23));
        Assert.False(sut.Remove(18));
        Assert.Equal([17, 20], sut.EnumerateIndices());

        Assert.True(sut.Add(18));
        Assert.True(sut.Remove(20));
        Assert.Equal([17, 18], sut.EnumerateIndices());
    }
}
