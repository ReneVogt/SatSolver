using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Revo.SatSolver.DataStructures;

sealed class StampArray(int _initialCapacity) : IEnumerable<int>
{
    int[] _buffer = new int[_initialCapacity];
    int _currentStamp = 1;

    public int Count { get; private set; }

    public StampArray() : this(1024)
    { }

    public bool Add(int index)
    {
        CheckArraySize(index);
        if (_buffer[index] == _currentStamp) return false;
        _buffer[index] = _currentStamp;
        Count++;
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
        CheckStampOverflow();
    }

    public IEnumerable<int> EnumerateIndices()
    {
        for (int i = 0, count = 0; count<Count; i++)
            if (_buffer[i] == _currentStamp)
            {
                count++;
                yield return i;
            }
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

    public IEnumerator<int> GetEnumerator() => EnumerateIndices().GetEnumerator();
    [ExcludeFromCodeCoverage]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

