namespace Revo.SatSolver.DataStructures;

sealed class CandidateHeap
{
    const int Arity = 2;
    const int Log2Arity = 1;

    readonly Variable[] _nodes;
    readonly int[] _indices;
    int _size;

#if DEBUG
    static CandidateHeap()
    {
        System.Diagnostics.Debug.Assert(Log2Arity > 0 && Math.Pow(2, Log2Arity) == Arity);
    }
#endif

    public CandidateHeap(Variable[] variables)
    {
        _nodes = (Variable[])variables.Clone();
        _indices = [.. Enumerable.Range(0, _nodes.Length)];
        _size = _nodes.Length;
        Heapify();
    }

    public void Enqueue(Variable variable)
    {
        var index = _indices[variable.Index];
        if (index < 0)
        {
            _size++;
            MoveUp(variable, _size-1);
            return;
        }

        if (index == 0)
        {
            MoveDown(variable, 0);
            return;
        }

        if (index == _size-1)
        {
            MoveUp(variable, index);
            return;
        }

        var parent = _nodes[GetParentIndex(index)];
        if (parent.Activity < variable.Activity)
            MoveUp(variable, index);
        else
            MoveDown(variable, index);
    }
    public Variable? Dequeue()
    {
        var nodes = _nodes;
        var indices = _indices;
        while (_size > 0)
        {
            var variable = nodes[0];
            RemoveRootNode();
            indices[variable.Index] = -1;
            if (variable.Sense is null) return variable;
        }

        return null;
    }

    void RemoveRootNode()
    {
        var lastNodeIndex = --_size;
        if (lastNodeIndex > 0)
            MoveDown(_nodes[lastNodeIndex], 0);
    }

    void Heapify()
    {
        var nodes = _nodes;
        var lastParentWithChildren = GetParentIndex(_size - 1);
        for (var index = lastParentWithChildren; index >= 0; --index)
            MoveDown(nodes[index], index);
    }
    void MoveUp(Variable variable, int nodeIndex)
    {
        var nodes = _nodes;
        var indices = _indices;
        while (nodeIndex > 0)
        {
            var parentIndex = GetParentIndex(nodeIndex);
            var parent = nodes[parentIndex];
            if (variable.Activity <= parent.Activity) break;
            
            nodes[nodeIndex] = parent;
            indices[parent.Index] = nodeIndex;
            nodeIndex = parentIndex;
        }

        nodes[nodeIndex] = variable;
        indices[variable.Index] = nodeIndex;
    }
    void MoveDown(Variable variable, int nodeIndex)
    {
        var nodes = _nodes;
        var indices = _indices;
        var size = _size;

        int i;
        while ((i = GetFirstChildIndex(nodeIndex)) < size)
        {
            var maxChild = nodes[i];
            var maxChildIndex = i;

            var childIndexUpperBound = Math.Min(i + Arity, size);
            while (++i < childIndexUpperBound)
            {
                var nextChild = nodes[i];
                if (nextChild.Activity <= maxChild.Activity) continue;
                maxChild = nextChild;
                maxChildIndex = i;
            }

            if (variable.Activity >= maxChild.Activity) break;

            nodes[nodeIndex] = maxChild;
            indices[maxChild.Index] = nodeIndex;
            nodeIndex = maxChildIndex;
        }

        nodes[nodeIndex] = variable;
        indices[variable.Index] = nodeIndex;
    }

    static int GetParentIndex(int index) => index - 1 >> Log2Arity;
    static int GetFirstChildIndex(int index) => (index << Log2Arity) + 1;
}
