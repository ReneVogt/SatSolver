using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;

namespace SatSolverTests.Processors;

public sealed partial class ConstraintMinimizerTests
{
    [Fact]
    public void Minimize_ThreeRootLiteralsBeforeUip_RemovesAllInOneCall()
    {
        var graph = new MinimizationGraph();
        var roots = Enumerable.Range(0, 3).Select(id => graph.Imply(id, true)).ToArray();
        var uip = graph.Decide(3, 1);

        graph.Check(new ConstraintMinimizer(), uip, roots, []);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void Minimize_RootAndRecursiveCandidatesInDifferentIdOrders_RemovesEveryRedundantLiteral(int order, bool mixedSigns)
    {
        // StampArray enumerates by ID, not insertion order. These mappings place
        // roots at the beginning, middle and end, and also cross its initial capacity.
        int[][] orders = [[0, 1, 2, 3, 4, 5, 6], [6, 2, 4, 0, 5, 3, 1], [7000, 5000, 3000, 1000, 0, 9000, 2000]];
        var ids = orders[order];
        var graph = new MinimizationGraph();
        var root1 = graph.Imply(ids[0], !mixedSigns);
        var root2 = graph.Imply(ids[1], true);
        var anchor = graph.Decide(ids[2], 1, !mixedSigns);
        var intermediate = graph.Imply(ids[3], true, anchor, root1);
        var first = graph.Imply(ids[4], !mixedSigns, intermediate);
        var second = graph.Imply(ids[5], true, first, root2);
        var uip = graph.Decide(ids[6], 2, !mixedSigns);

        graph.Check(new ConstraintMinimizer(), uip, [root1, root2, anchor, first, second], [anchor]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Minimize_UnitLearnedClause_PreservesUip(bool sense)
    {
        var graph = new MinimizationGraph();
        var uip = graph.Decide(0, 1, sense);

        graph.Check(new ConstraintMinimizer(), uip, [], []);
    }

    [Fact]
    public void Minimize_DecisionsWithoutReasons_KeepsEveryLiteral()
    {
        var graph = new MinimizationGraph();
        var first = graph.Decide(0, 1);
        var second = graph.Decide(1, 2, false);
        var uip = graph.Decide(2, 3);

        graph.Check(new ConstraintMinimizer(), uip, [first, second], [first, second]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Minimize_DirectReasonAndRootDependency_RemovesCandidate(bool reverseReason)
    {
        var graph = new MinimizationGraph(reverseReason);
        var root = graph.Imply(4, false);
        var anchor = graph.Decide(1, 1, false);
        var candidate = graph.Imply(0, true, anchor, root);
        var uip = graph.Decide(2, 2);

        graph.Check(new ConstraintMinimizer(), uip, [candidate, anchor], [anchor]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Minimize_RecursiveReason_RequiresEveryExternalLeafToBeCovered(bool includeExternalDecision)
    {
        var graph = new MinimizationGraph();
        var anchor = graph.Decide(1, 1);
        var external = graph.Decide(2, 2, false);
        var middle = graph.Imply(4, false, anchor, external);
        var candidate = graph.Imply(0, true, middle);
        var uip = graph.Decide(3, 3);

        graph.Check(new ConstraintMinimizer(), uip,
            includeExternalDecision ? [candidate, anchor, external] : [candidate, anchor],
            includeExternalDecision ? [anchor, external] : [candidate, anchor]);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Minimize_SharedDiamondAndRepeatedDependencies_RemovesOnlyProvenCandidates(bool blocked, bool reverseReasons)
    {
        var graph = new MinimizationGraph(reverseReasons);
        var anchor = graph.Decide(2, 1);
        var shared = graph.Imply(4, false, anchor);
        var left = graph.Imply(5, true, shared);
        var external = blocked ? graph.Decide(6, 2) : shared;
        var right = graph.Imply(7, false, external);
        var first = graph.Imply(0, true, left, right, shared);
        var second = graph.Imply(1, false, right, shared);
        var uip = graph.Decide(3, blocked ? 3 : 2);

        // Both successful and unsuccessful subgraphs are reused by the second
        // candidate. Reversing reasons also puts the failing branch first/last.
        graph.Check(new ConstraintMinimizer(), uip, [first, second, anchor],
            blocked ? [first, second, anchor] : [anchor]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Minimize_PropagatedUip_PreservesAssertingLiteral(bool sense)
    {
        var graph = new MinimizationGraph();
        var anchor = graph.Decide(1, 1, !sense);
        var redundant = graph.Imply(0, sense, anchor);
        var decision = graph.Decide(4, 2, sense);
        var uip = graph.Imply(2, !sense, decision, anchor);

        graph.Check(new ConstraintMinimizer(), uip, [redundant, anchor], [anchor]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Minimize_ReusedInstance_DoesNotReuseProofsFromEarlierCalls(bool initiallyRedundant)
    {
        var sut = new ConstraintMinimizer();
        foreach (var redundant in new[] { initiallyRedundant, !initiallyRedundant, initiallyRedundant })
        {
            // Deliberately reuse IDs with different reasons and decision levels.
            var graph = new MinimizationGraph();
            var anchor = graph.Decide(1, 1);
            var external = redundant ? anchor : graph.Decide(4, 2);
            var middle = graph.Imply(5, false, external);
            var candidate = graph.Imply(0, true, middle);
            var uip = graph.Decide(2, redundant ? 2 : 3);

            graph.Check(sut, uip, [candidate, anchor], redundant ? [anchor] : [candidate, anchor]);
        }
    }

    [Fact]
    public void Minimize_MoreThanInitialBufferCapacity_ProcessesAllCandidates()
    {
        var graph = new MinimizationGraph();
        var roots = Enumerable.Range(0, 1025).Select(id => graph.Imply(id, (id & 1) == 0)).ToArray();
        var uip = graph.Decide(1025, 1);

        graph.Check(new ConstraintMinimizer(), uip, roots, [], exhaustive: false);
    }

    sealed class MinimizationGraph(bool reverseReasons = false)
    {
        readonly List<Variable> _trail = [];
        readonly List<ConstraintLiteral[]> _formula = [];

        public Variable Decide(int id, int level, bool sense = true)
        {
            Assert.True(level > 0);
            Assert.DoesNotContain(_trail, variable => variable.Reason is null && variable.DecisionLevel == level);
            return Assign(id, level, sense);
        }

        public Variable Imply(int id, bool sense, params Variable[] antecedents)
        {
            Assert.All(antecedents, antecedent => Assert.Contains(antecedent, _trail));
            var level = antecedents.Select(variable => variable.DecisionLevel).DefaultIfEmpty(0).Max();
            var variable = Assign(id, level, sense);
            ConstraintLiteral[] reason = [.. antecedents.Select(Falsified), Satisfied(variable)];
            if (reverseReasons) Array.Reverse(reason);
            variable.Reason = new Constraint(reason, reason[0], reason[^1]);
            _formula.Add(reason);
            return variable;
        }

        Variable Assign(int id, int level, bool sense)
        {
            Assert.DoesNotContain(_trail, variable => variable.Index == id);
            Assert.True(_trail.Count == 0 || _trail[^1].DecisionLevel <= level, "Assignments must follow trail order.");
            var variable = new Variable(id) { Sense = sense, DecisionLevel = level };
            _trail.Add(variable);
            return variable;
        }

        public void Check(ConstraintMinimizer sut, Variable uip, Variable[] candidates, Variable[] expected, bool exhaustive = true)
        {
            Assert.All(candidates, candidate => Assert.True(candidate.DecisionLevel < uip.DecisionLevel));
            Assert.Equal(uip, _trail[^1]);

            // Construct an actual first-UIP conflict. Resolving the two children
            // against (not child1 OR not child2 OR candidate literals) yields
            // exactly the input learned clause. Decisions are NOT formula axioms.
            var nextId = _trail.Max(variable => variable.Index) + 1;
            var child1 = Imply(nextId, true, uip);
            var child2 = Imply(nextId + 1, false, uip);
            _formula.Add([.. candidates.Select(Falsified), Falsified(child1), Falsified(child2)]);
            ValidateReasons();

            ConstraintLiteral[] original = [.. candidates.Select(Falsified), Falsified(uip)];
            Assert.All(original, literal => Assert.Equal(false, literal.Sense));
            Assert.All(_formula[^1], literal => Assert.Equal(false, literal.Sense));
            Assert.Single(original, literal => literal.Variable.DecisionLevel == uip.DecisionLevel);

            var knownLiterals = new ConstraintLiteral[(_trail.Max(variable => variable.Index) + 1) * 2];
            foreach (var variable in _trail)
            {
                knownLiterals[variable.PositiveLiteral.StampIndex] = variable.PositiveLiteral;
                knownLiterals[variable.NegativeLiteral.StampIndex] = variable.NegativeLiteral;
            }
            var learned = new StampArray();
            foreach (var literal in original) Assert.True(learned.Add(literal.StampIndex));

            sut.MinimizeConstraint(learned, uip.DecisionLevel, knownLiterals);

            var result = learned.Select(index => knownLiterals[index]).ToArray();
            Assert.All(result, literal => Assert.Contains(literal, original));
            Assert.Contains(Falsified(uip), result);
            AssertAssertingAfterBackjump(result, uip);
            if (exhaustive) AssertEntailed(original, result);
            Assert.Equal(expected.Append(uip).Select(variable => Falsified(variable).StampIndex).Order(), learned);
        }

        void ValidateReasons()
        {
            var preceding = new HashSet<Variable>();
            foreach (var variable in _trail)
            {
                if (variable.Reason is { } reason)
                {
                    Assert.Equal(Satisfied(variable), Assert.Single(reason.Literals, literal => literal.Sense == true));
                    Assert.All(reason.Literals.Where(literal => literal.Variable != variable), literal =>
                    {
                        Assert.Equal(false, literal.Sense);
                        Assert.Contains(literal.Variable, preceding);
                        Assert.True(literal.Variable.DecisionLevel <= variable.DecisionLevel);
                    });
                }
                else Assert.True(variable.DecisionLevel > 0);
                preceding.Add(variable);
            }
        }

        static void AssertAssertingAfterBackjump(ConstraintLiteral[] result, Variable uip)
        {
            var backjumpLevel = result.Where(literal => literal.Variable != uip)
                .Select(literal => literal.Variable.DecisionLevel).DefaultIfEmpty(0).Max();
            Assert.True(backjumpLevel < uip.DecisionLevel);

            // Independently project the trail onto the backjump level. Higher
            // assignments become unassigned; retained literal values stay false.
            bool? AfterBackjump(ConstraintLiteral literal) =>
                literal.Variable.DecisionLevel > backjumpLevel ? null : literal.Sense;

            Assert.Equal(Falsified(uip), Assert.Single(result, literal => AfterBackjump(literal) is null));
            Assert.All(result.Where(literal => literal.Variable != uip), literal => Assert.Equal(false, AfterBackjump(literal)));
        }

        void AssertEntailed(ConstraintLiteral[] original, ConstraintLiteral[] result)
        {
            Assert.True(_trail.Count <= 18, "Only small graphs should use the exhaustive oracle.");
            // Map sparse IDs to dense bit positions; evaluate literal orientation,
            // not Sense (which represents the conflicting trail assignment).
            var positions = _trail.Select((variable, index) => (variable, index))
                .ToDictionary(pair => pair.variable, pair => pair.index);
            var models = 0;
            for (var assignment = 0; assignment < 1 << _trail.Count; assignment++)
            {
                bool Satisfies(ConstraintLiteral[] clause) => clause.Any(literal =>
                    ((assignment & (1 << positions[literal.Variable])) != 0) == literal.Orientation);

                if (!_formula.All(Satisfies)) continue;
                models++;
                Assert.True(Satisfies(original), $"Input clause is not entailed, assignment {assignment}.");
                Assert.True(Satisfies(result), $"Minimized clause is not entailed, assignment {assignment}.");
            }
            Assert.True(models > 0, "Entailment must not hold vacuously for an unsatisfiable fixture.");
        }

        static ConstraintLiteral Satisfied(Variable variable) => variable.Sense == true ? variable.PositiveLiteral : variable.NegativeLiteral;
        static ConstraintLiteral Falsified(Variable variable) => variable.Sense == true ? variable.NegativeLiteral : variable.PositiveLiteral;
    }
}
