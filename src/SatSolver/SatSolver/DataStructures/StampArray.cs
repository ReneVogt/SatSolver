using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Revo.SatSolver.DataStructures;

sealed class StampArray(int _initialCapacity) : IEnumerable<int>
{
    struct Enumerator(StampArray _parent) : IEnumerator<int>, IEnumerator
    {
        readonly int _version = _parent._version;
        readonly int _total = _parent.Count;
        readonly int[] _buffer = _parent._buffer;
        readonly int _stamp = _parent._currentStamp;
        int _index = -1, _count;

        public int Current { get; private set; } = -1;
        [ExcludeFromCodeCoverage]
        readonly object IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_version != _parent._version)
                throw new InvalidOperationException($"The {nameof(StampArray)} was changed during enumeration.");
            
            if (_count == _total) return false;

            _index++;
            while (_buffer[_index] != _stamp) _index++;
            
            _count++;
            Current = _index;
            return true;
        }

        [ExcludeFromCodeCoverage]
        public readonly void Dispose() { }
        [ExcludeFromCodeCoverage]
        public void Reset()
        {
            Current = default;
            _index = -1;
            _count = 0;
        }
    }

    int _version;

    int[] _buffer = new int[_initialCapacity];
    int _currentStamp = 1;

    public int Count { get; private set; }

    public StampArray() : this(1024)
    { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(int index)
    {
        CheckArraySize(index);
        if (_buffer[index] == _currentStamp) return false;
        _buffer[index] = _currentStamp;
        Count++;
        _version++;
        return true;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(int index)
    {
        if (!Contains(index)) return false;
        _buffer[index] = 0;
        Count--;
        _version++;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(int index) => _buffer.Length > index && _buffer[index] == _currentStamp;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _version++;
        _currentStamp++;
        Count = 0;
        CheckStampOverflow();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void CheckArraySize(int required)
    {
        if (required >= _buffer.Length)
            Array.Resize(ref _buffer, required << 1);
    }
    [ExcludeFromCodeCoverage]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void CheckStampOverflow()
    {
        if (_currentStamp < int.MaxValue) return;        
        Array.Clear(_buffer);
        _currentStamp = 1;        
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<int> GetEnumerator() => new Enumerator(this);
    [ExcludeFromCodeCoverage]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}