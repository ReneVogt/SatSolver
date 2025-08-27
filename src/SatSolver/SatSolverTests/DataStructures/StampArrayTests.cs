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
    public void Add_Resize()
    {
        var sut = new StampArray(16);
        Assert.True(sut.Add(1000));
        Assert.Equal(1, sut.Count);
        Assert.True(sut.Contains(1000));
        Assert.True(sut.Remove(1000));
        Assert.Equal(0, sut.Count);
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

        Assert.Equal([17, 18, 20], sut);

        Assert.True(sut.Remove(18));
        Assert.Equal([17, 20], sut);

        sut.Clear();
        Assert.Empty(sut);
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

        Assert.Equal([17, 18, 20], sut);

        Assert.True(sut.Remove(18));
        Assert.Equal(2, sut.Count);
        Assert.False(sut.Remove(23));
        Assert.False(sut.Remove(18));
        Assert.Equal([17, 20], sut);

        Assert.True(sut.Add(18));
        Assert.Equal(3, sut.Count);
        Assert.True(sut.Remove(20));
        Assert.Equal(2, sut.Count);
        Assert.Equal([17, 18], sut);
    }

    [Fact]
    public void Contains_ToolargeIndex()
    {
        var sut = new StampArray(32)
        {
            17,
            18,
            20
        };

        Assert.False(sut.Contains(64));
    }

    [Fact]
    public void AddDuringEnumeration_Throws()
    {
        var sut = new StampArray
        {
            3, 5, 12, 18, 20
        };

        using var enumerator = sut.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        sut.Add(13);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }
    [Fact]
    public void RemoveDuringEnumeration_Throws()
    {
        var sut = new StampArray
        {
            3, 5, 12, 18, 20
        };

        using var enumerator = sut.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        sut.Remove(12);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }
    [Fact]
    public void ClearDuringEnumeration_Throws()
    {
        var sut = new StampArray
        {
            3, 5, 12, 18, 20
        };

        using var enumerator = sut.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        sut.Clear();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }
}
