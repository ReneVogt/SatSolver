using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;

namespace SatSolverTests.Processors;

public sealed partial class ConstraintMinimizerTests
{
    [Theory]
    [InlineData(12, false)]
    [InlineData(13, false)]
    [InlineData(12, true)]
    [InlineData(13, true)]
    public void Minimize_ReasonSizeLimit_KeepsCandidateOnlyWhenLimitIsExceeded(int reasonSize, bool nested)
    {
        var graph = new MinimizationGraph();
        var roots = Enumerable.Range(4, reasonSize - 2).Select(id => graph.Imply(id, false)).ToArray();
        var anchor = graph.Decide(1, 1);
        var largeReason = graph.Imply(nested ? 20 : 0, true, [anchor, .. roots]);
        var candidate = nested ? graph.Imply(0, false, largeReason) : largeReason;
        var uip = graph.Decide(2, 2);

        graph.Check(new ConstraintMinimizer(), uip, [candidate, anchor],
            reasonSize == 12 ? [anchor] : [candidate, anchor]);
    }

    [Theory]
    [InlineData(80, false)]
    [InlineData(300, true)]
    public void Minimize_StackBudget_StopsConservativelyAndContinuesWithNextCandidate(int chainLength, bool exhausted)
    {
        var graph = new MinimizationGraph();
        var anchor = graph.Decide(2, 1);
        var candidate = anchor;
        for (var i = 0; i < chainLength; i++)
            candidate = graph.Imply(i == chainLength - 1 ? 0 : 4 + i, true, candidate);
        var easy = graph.Imply(1, false, anchor);
        var uip = graph.Decide(3, 2);

        // Four learned literals give a stack limit of 256. Binary reasons
        // require only 4 visits per node (at most 1200), below the 3000 visit
        // budget. Thus only the stack limit can stop the longer chain.
        var sut = new ConstraintMinimizer();
        graph.Check(sut, uip, [candidate, easy, anchor],
            exhausted ? [candidate, anchor] : [anchor], exhaustive: false);
        CheckReuseAfterBudgetTraversal(sut);
    }

    [Theory]
    [InlineData(8, false)]
    [InlineData(9, true)]
    public void Minimize_VisitBudget_StopsConservativelyAndContinuesWithNextCandidate(int width, bool exhausted)
    {
        var graph = new MinimizationGraph();
        var anchor = graph.Decide(2, 1);
        var nextId = 4;
        var candidate = BuildReasonTree(graph, anchor, 3, width, ref nextId, 0);
        var easy = graph.Imply(1, false, anchor);
        var uip = graph.Decide(3, 2);

        // A depth-3 tree has width^3 leaves with binary reasons (4 visits each)
        // and 1 + width + width^2 internal nodes (width + 3 visits each).
        // Width 8: 2851 visits. Width 9: 4008 visits, exceeding 3000.
        // Reasons have at most 10 literals; DFS stack depth is at most 4,
        // far below 64 * 4. Neither of the other limits is reached.
        var sut = new ConstraintMinimizer();
        graph.Check(sut, uip, [candidate, easy, anchor],
            exhausted ? [candidate, anchor] : [anchor], exhaustive: false);
        CheckReuseAfterBudgetTraversal(sut);
    }

    [Fact]
    public void Minimize_VisitBudgetIsPerCandidate_RemovesBothIndependentlyAffordableCandidates()
    {
        var graph = new MinimizationGraph();
        var anchor = graph.Decide(2, 1);
        var nextId = 4;
        var first = BuildReasonTree(graph, anchor, 3, 8, ref nextId, 0);
        var second = BuildReasonTree(graph, anchor, 3, 8, ref nextId, 1);
        var uip = graph.Decide(3, 2);

        // Each disjoint tree needs 2851 visits, but together they need 5702.
        // Their only shared dependency is the anchor already in the learned
        // clause, so a cached proof cannot make the second traversal cheaper.
        graph.Check(new ConstraintMinimizer(), uip, [first, second, anchor], [anchor], exhaustive: false);
    }

    static Variable BuildReasonTree(MinimizationGraph graph, Variable anchor, int depth, int width, ref int nextId, int? rootId = null)
    {
        if (depth == 0) return graph.Imply(nextId++, true, anchor);

        var children = new Variable[width];
        for (var i = 0; i < children.Length; i++)
            children[i] = BuildReasonTree(graph, anchor, depth - 1, width, ref nextId);
        return graph.Imply(rootId ?? nextId++, true, children);
    }

    static void CheckReuseAfterBudgetTraversal(ConstraintMinimizer sut)
    {
        var graph = new MinimizationGraph();
        // ID 4 was a visited dependency, and in a wide tree already proved
        // redundant before budget exhaustion. It is now a necessary decision.
        var necessary = graph.Decide(4, 1);
        var anchor = graph.Decide(6, 2);
        var removable = graph.Imply(0, false, anchor);
        var uip = graph.Decide(7, 3);

        graph.Check(sut, uip, [necessary, anchor, removable], [necessary, anchor]);
    }
}
