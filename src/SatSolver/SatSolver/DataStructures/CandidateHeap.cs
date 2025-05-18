namespace Revo.SatSolver.DataStructures;

/// <summary>
/// A max heap containing the candidate variables.
/// Only candidates will be dequeued. Propagated
/// units that are re-enqueued are updated in 
/// place. We keep the <see cref="_indices"/> array
/// to find the node in O(1).
/// NOTE: using only Variables in the nodes without
/// explicit activity turns out to be slower for 
/// whatever reason. Rescaling appears so rarely 
/// that it does not count. So please don't
/// refactor to use only a Variable[] for nodes.
/// </summary>
sealed class CandidateHeap
{
    const int Arity = 2;
    const int Log2Arity = 1;

    readonly Variable[] _variables;
    readonly (int Variable, double Activity)[] _nodes;
    readonly int[] _indices;
    int _size;

    public int Count => _size;

#if DEBUG
    static CandidateHeap()
    {
        System.Diagnostics.Debug.Assert(Log2Arity > 0 && Math.Pow(2, Log2Arity) == Arity);
    }
#endif

    public CandidateHeap(Variable[] variables)
    {
        _variables = variables;
        _nodes = [.. _variables.Select(v => (v.Index, v.Activity))];
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
            MoveUp((variable.Index, variable.Activity), _size-1);
            return;
        }

        var lastActivity = _nodes[index].Activity;
        _nodes[index].Activity = variable.Activity;
        var cmp = variable.Activity.CompareTo(lastActivity);
        if (cmp > 0)
            MoveUp(_nodes[index], index);
        else if (cmp < 0)
            MoveDown(_nodes[index], index);
    }
    public Variable? Dequeue()
    {
        var variables = _variables;
        while (_size > 0)
        {
            var index = _nodes[0].Variable;
            RemoveRootNode();
            _indices[index] = -1;
            var variable = variables[index];
            if (variable.Sense is null) return variable;
        }

        return null;
    }

    public void Rescale(double scaleLimit)
    {
        var nodes = _nodes;
        for(var i=0; i<_indices.Length; i++)
        {
            var index = _indices[i];
            if (index < 0) continue;
            nodes[index].Activity /= scaleLimit;
        }
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
    void MoveUp((int Variable, double Activity) node, int nodeIndex)
    {
        var nodes = _nodes;
        var indices = _indices;
        while (nodeIndex > 0)
        {
            var parentIndex = GetParentIndex(nodeIndex);
            var parent = nodes[parentIndex];
            if (node.Activity <= parent.Activity) break;
            
            nodes[nodeIndex] = parent;
            indices[parent.Variable] = nodeIndex;
            nodeIndex = parentIndex;
        }

        nodes[nodeIndex] = node;
        indices[node.Variable] = nodeIndex;
    }
    void MoveDown((int Variable, double Activity) node, int nodeIndex)
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

            if (node.Activity >= maxChild.Activity) break;

            nodes[nodeIndex] = maxChild;
            indices[maxChild.Variable] = nodeIndex;
            nodeIndex = maxChildIndex;
        }

        nodes[nodeIndex] = node;
        indices[node.Variable] = nodeIndex;
    }

    static int GetParentIndex(int index) => index - 1 >> Log2Arity;
    static int GetFirstChildIndex(int index) => (index << Log2Arity) + 1;
}
