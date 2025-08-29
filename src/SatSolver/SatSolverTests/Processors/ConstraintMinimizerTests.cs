using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace SatSolverTests.Processors;

public sealed class ConstraintMinimizerTests
{
    readonly ConstraintFactory _constraintFactory = new([]);
    [Fact]
    public void FourStateSudoku_FirstConflict_NoMinimization()
    {
        var variables = Enumerable.Range(0, 16).Select(i => new Variable(i)).ToArray();
        var literals = new ConstraintLiteral[variables.Length*2];
        for (var i=0; i<variables.Length; i++)
        {
            literals[i<<1] = variables[i].PositiveLiteral;
            literals[(i<<1)+1] = variables[i].NegativeLiteral;
        }

        variables[0].Sense = true;
        variables[0].DecisionLevel = 1;
        variables[1].Sense = false;
        variables[1].DecisionLevel = 1;
        variables[1].Reason = _constraintFactory.CreateInitialConstraint([variables[0].NegativeLiteral, variables[1].NegativeLiteral]);
        variables[2].Sense = false;
        variables[2].DecisionLevel = 1;
        variables[2].Reason = _constraintFactory.CreateInitialConstraint([variables[0].NegativeLiteral, variables[2].NegativeLiteral]);
        
        variables[4].Sense = true;
        variables[4].DecisionLevel = 2;
        variables[5].Sense = false;
        variables[5].DecisionLevel = 2;
        variables[5].Reason = _constraintFactory.CreateInitialConstraint([variables[4].NegativeLiteral, variables[5].NegativeLiteral]);
        variables[6].Sense = false;
        variables[6].DecisionLevel = 2;
        variables[6].Reason = _constraintFactory.CreateInitialConstraint([variables[4].NegativeLiteral, variables[6].NegativeLiteral]);

        variables[8].Sense = true;
        variables[8].DecisionLevel = 3;

        var learned = new StampArray
        {
            variables[1].NegativeLiteral.StampIndex,
            variables[2].NegativeLiteral.StampIndex,
            variables[5].NegativeLiteral.StampIndex,
            variables[6].NegativeLiteral.StampIndex,
            variables[8].NegativeLiteral.StampIndex
        };

        var copy = new StampArray();
        foreach (var index in learned) copy.Add(index);

        var sut = new ConstraintMinimizer();
        sut.MinimizeConstraint(learned, 3, literals);
        Assert.Equal(copy, learned);
    }
    [Fact]
    public void SimpleMinimizableExample()
    {
        var variables = Enumerable.Range(0, 4).Select(i => new Variable(i)).ToArray();
        var literals = new ConstraintLiteral[variables.Length*2];
        for (var i = 0; i<variables.Length; i++)
        {
            literals[i<<1] = variables[i].PositiveLiteral;
            literals[(i<<1)+1] = variables[i].NegativeLiteral;
        }

        variables[0].Sense = true;
        variables[0].DecisionLevel = 1;
        variables[1].Sense = true;
        variables[1].DecisionLevel = 2;
        variables[2].Sense = true;
        variables[2].DecisionLevel = 2;
        variables[2].Reason = _constraintFactory.CreateInitialConstraint([variables[0].NegativeLiteral, variables[1].NegativeLiteral, variables[2].PositiveLiteral]);
        variables[3].Sense = true;
        variables[3].DecisionLevel = 3;
        var learned = new StampArray
        {
            variables[0].NegativeLiteral.StampIndex,
            variables[1].NegativeLiteral.StampIndex,
            variables[2].NegativeLiteral.StampIndex,
            variables[3].NegativeLiteral.StampIndex,
        };

        var sut = new ConstraintMinimizer();
        sut.MinimizeConstraint(learned, 3, literals);
        Assert.Equal([
            variables[0].NegativeLiteral.StampIndex,
            variables[1].NegativeLiteral.StampIndex,
            variables[3].NegativeLiteral.StampIndex],
            learned);
    }
}
