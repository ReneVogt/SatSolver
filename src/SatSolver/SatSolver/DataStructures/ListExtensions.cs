namespace Revo.SatSolver.DataStructures;

static class ListExtensions
{
    public static void SwapRemove(this List<Constraint> list, int index)
    {
        var last = list.Count - 1;
        if (last != index)
            list[index] = list[last];
        list.RemoveAt(last);        
    }
}
