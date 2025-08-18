namespace Revo.SatSolver.DataStructures;

sealed class StampArray
{
    int[] _buffer = new int[1024];
    int _currentStamp = 1;
    int _maximum = -1;

    public int Count { get; private set; }
    public bool Add(int index)
    {
        if (index >= _buffer.Length)
            Array.Resize(ref _buffer, index*2);
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
        return true;
    }

    public bool Contains(int index) => _buffer.Length > index && _buffer[index] == _currentStamp;
    public void Clear()
    {
        _currentStamp++;
        Count = 0;
        _maximum = -1;
        if (_currentStamp == int.MaxValue)
        {
            Array.Clear(_buffer);
            _currentStamp = 1;
        }
    }

    public IEnumerable<int> EnumerateIndices()
    {
        for(var i=0; i<=_maximum; i++) if (_buffer[i] == _currentStamp) yield return i;
    }
}

