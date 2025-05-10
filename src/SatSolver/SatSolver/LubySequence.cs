namespace Revo.SatSolver;

sealed class LubySequence(long baseValue = 1)
{
    long _u = 1, _v = 1;

    public long Next()
    {
        //
        // Donald Knuth's 'reluctant doubling' formula
        //
        var next = _v;
        if ((_u & (-_u)) == _v)
        {
            _u += 1;
            _v = 1;
        } 
        else _v *= 2;

        return next * baseValue;
    }

    public static IEnumerable<long> Enumerate(long baseValue = 1)
    {
        var sequence = new LubySequence(baseValue);
        for (; ; ) yield return sequence.Next();
    }
}
