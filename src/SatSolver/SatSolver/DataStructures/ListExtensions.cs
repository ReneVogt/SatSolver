using System.Runtime.CompilerServices;

namespace Revo.SatSolver.DataStructures;

static class ListExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SwapRemove(this List<Constraint> list, int index)
    {
        var last = list.Count - 1;
        if (last != index)
            list[index] = list[last];
        list.RemoveAt(last);        
    }
}
