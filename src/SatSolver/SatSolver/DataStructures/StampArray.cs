using System.Diagnostics.CodeAnalysis;

namespace Revo.SatSolver.DataStructures;

sealed class StampArray(int _initialCapacity)
{
    int[] _buffer = new int[_initialCapacity];
    int _currentStamp = 1;
    int _maximum = -1;

    public int Count { get; private set; }

    public StampArray() : this(1024)
    { }

    public bool Add(int index)
    {
        CheckArraySize(index);
        if (_buffer[index] == _currentStamp) return false;
        _buffer[index] = _currentStamp;
        Count++;
        if (index > _maximum) _maximum = index;
        return true;
    }
    public bool Remove(int index)
    {
        if (!Contains(index)) return false;
        _buffer[index] = 0;
        Count--;
        return true;
    }

    public bool Contains(int index) => _buffer.Length > index && _buffer[index] == _currentStamp;
    public void Clear()
    {
        _currentStamp++;
        Count = 0;
        _maximum = -1;
        CheckStampOverflow();
    }

    public IEnumerable<int> EnumerateIndices()
    {
        for(var i=0; i<=_maximum; i++) if (_buffer[i] == _currentStamp) yield return i;
    }

    void CheckArraySize(int required)
    {
        if (required >= _buffer.Length)
            Array.Resize(ref _buffer, required << 1);
    }
    [ExcludeFromCodeCoverage]
    void CheckStampOverflow()
    {
        if (_currentStamp < int.MaxValue) return;        
        Array.Clear(_buffer);
        _currentStamp = 1;        
    }
}

