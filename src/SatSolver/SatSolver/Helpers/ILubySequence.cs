
namespace Revo.SatSolver.Helpers;

interface ILubySequence
{
    static abstract IEnumerable<long> Enumerate(long baseValue = 1);
    long Next();
}